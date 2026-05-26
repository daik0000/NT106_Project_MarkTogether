# Kế hoạch sửa lỗi OT — Đợt 2: IME tiếng Việt, paste/delete bulk, replace block, editor đơ

> Tài liệu kế hoạch CHI TIẾT cho AI implement. AI implement chỉ đọc file này + `OT_FIX_PLAN.md` (đợt 1) + `OT_FIX_IMPLEMENTATION_REPORT.md` + code thật rồi thực thi theo đúng thứ tự.
>
> ⚠️ KHÔNG được refactor lớn, KHÔNG đổi kiến trúc, KHÔNG đổi framework, KHÔNG đổi control `TextBox`, KHÔNG sửa module ngoài phạm vi nêu dưới.
>
> Tiền đề (đợt 1 đã làm):
> - `MaxCharsPerPacket = 32` cả client + server.
> - `HandleOpBroadcast` đã `SelectedText =` (không reset scroll), flush pending + cập nhật `_clientRevision` monotonic.
> - Server `ProcessRealtimeOp` có `currentClientRev` cục bộ, đã prewarm DocumentState ở `HandleDocOpen`.
> - `OTEngine.TransformDeleteAgainstInsert` đã xử lý insert nằm trong delete range.
>
> Đợt 2 KHÔNG được phá những fix đó.

---

## 0. Vấn đề mới và root cause đã verify từ code

### Vấn đề 1 — Gõ tiếng Việt (Telex/VNI/Unikey) đồng bộ chậm, remote nhấp nháy
- Unikey/EVKey KHÔNG dùng WM_IME thật của Windows. Chúng intercept WM_KEYDOWN, emit chuỗi `WM_CHAR (a)` rồi sau `f` (Telex) đẻ ra `WM_BACKSPACE + WM_CHAR (à)`.
- Mỗi tổ hợp dấu → `txtRawMarkdown_TextChanged` bắn ≥ 2 lần → `ComputeTextDelta` trả về `delete(1) + insert(1)` → `TrackRealtimeEditOps` đẩy 2 op riêng (Flush + Queue Delete, Flush + Queue Insert — line 596–602).
- Remote nhận: insert `a` → delete `a` → insert `á` ⇒ thấy nhấp nháy.

### Vấn đề 2 — Paste / xóa đoạn dài bị split từng dòng
- **Hai chỗ chunk thành 32 ký tự**:
  - `SendPendingChunksIfNeeded` (line 690) và `FlushPendingEditOperation` (line 715) trong pipeline typing.
  - `EnqueuePasteOperations` (line 1851) trong pipeline paste.
- Mỗi chunk được enqueue thành 1 entry trong `_pasteChunkQueue` → `PasteSenderLoop` gửi từng entry là 1 `OP_INSERT`/`OP_DELETE` packet riêng (line 1936–1944).
- Server `ProcessRealtimeOp` reject `charCount > 32` (`ClientHandler.cs:1144`) ⇒ client KHÔNG thể gửi 1000 ký tự / packet kể cả muốn.
- Remote nhận N broadcast → BeginInvoke N lần → cảm giác "từng dòng".

### Vấn đề 3 — Editor đơ
- Khi 2 user gõ tiếng Việt cùng lúc: A đẻ ~6 op/giây (do delete+insert nhân đôi) × paste / xóa block tạo burst chunk 32 → queue dài → `PasteSenderLoop` đợi reply sync `5000ms` mỗi chunk (`SocketClient.SendInsertOps` line 470, `timeoutMs: 5000`). Nếu server slow / queue dài → loop blocking.
- `HandleOpBroadcast` đọc `txtRawMarkdown.Text` 3 lần / op (lấy `current` ở line 800, fire TextChanged ở 807/817 → handler ở line 489/493 lại đọc Text). Với doc 50KB + 32 op broadcast: ~5MB clone + GC pressure.
- `FlushPendingEditOperation` bị gọi liên tục do delta tiếng Việt đan xen Insert/Delete trigger `QueueOperation` line 640 `if (_pendingOpType.Value != opType) FlushPendingEditOperation();`.

---

## 1. Quy trình bắt buộc cho AI implement

PHẢI theo đúng thứ tự:

1. **Đọc trước**: `OT_FIX_PLAN.md`, `OT_FIX_IMPLEMENTATION_REPORT.md`, `DOCUMENTATION.md` (mục 7.9, 10, 17 — phần OT).
2. **Phase A** — thêm log, reproduce 3 vấn đề, thu log, STOP báo cáo log.
3. **Phase B** — implement `ClassifyEditCase` (kiến trúc xương sống).
4. **Phase C–F** — code từng case (typing / IME / BulkInsert / BulkDelete / Replace block).
5. **Phase G** — tăng giới hạn server cho bulk packet.
6. **Phase H** — tối ưu remote apply (`HandleOpBroadcast`).
7. **Phase I** — test toàn bộ T1–T15.
8. **Cập nhật `DOCUMENTATION.md` + `OT_FIX_PLAN.md`** + tạo `OT_FIX_IMPLEMENTATION_REPORT_V2.md`.

---

## 2. File liên quan (chỉ đụng những file này)

| File | Vai trò trong đợt 2 |
|------|---------------------|
| `MarkTogether.Client/TypeRenderForm.cs` | Toàn bộ pipeline editor: TextChanged, ComputeTextDelta, QueueOperation, FlushPendingEditOperation, EnqueuePasteOperations, PasteSenderLoop, HandleOpBroadcast, PerformFastPaste |
| `MarkTogether.Server/Network/ClientHandler.cs` | `ProcessRealtimeOp`: tăng limit theo opCount, batch broadcast |
| `MarkTogether.Client/Network/SocketClient.cs` | (chỉ đọc) verify `SendInsertOps/SendDeleteOps` gửi list nguyên vẹn |
| `MarkTogether.Shared/Packet.cs` | (chỉ đọc) verify `ops: List<EditOpItem>` — KHÔNG sửa |
| `MarkTogether.Server/OT/*` | (chỉ đọc) verify TransformAndApply không có giới hạn nội tại — KHÔNG sửa |

**KHÔNG đụng**: `OTEngine.cs`, `DocumentState.cs`, `DocumentStateManager.cs`, `SessionManager.cs`, `Gateway/*`, repository, UI khác.

---

## 3. Kiến trúc pipeline MỚI (xương sống cho đợt 2)

### Pipeline hiện tại (cần thay)

```
TextChanged
  → TrackRealtimeEditOps
    → ComputeTextDelta
    → QueueOperation         ← gom theo Insert/Delete
      → _pendingOpText
        → SendPendingChunksIfNeeded (chunk 32)
          → _pasteChunkQueue
            → PasteSenderLoop (1 packet / chunk / op)
```

### Pipeline MỚI (đợt 2)

```
TextChanged
  → DebounceTimer (chỉ cho typing nhỏ)
    → ComputeTextDelta
      → ClassifyEditCase
        ├── Typing      → QueueOperation (cũ, chunk 32)
        ├── BulkInsert  → SendBulkOp(Insert) — 1 op lớn / packet (≤ BulkMax)
        ├── BulkDelete  → SendBulkOp(Delete) — 1 op lớn / packet (≤ BulkMax)
        └── ReplaceBlock→ SendBulkOp(Delete) rồi SendBulkOp(Insert)

PerformFastPaste (Ctrl+V intercept)
  → Bỏ qua TextChanged, gọi trực tiếp pipeline BulkInsert / ReplaceBlock
```

### Hằng số mới

```csharp
private const int TypingMaxCharsPerPacket = 32;   // realtime nhỏ — giữ nguyên
private const int BulkMaxCharsPerPacket   = 8192; // paste/delete bulk
private const int TypingDebounceMs        = 120;  // IME tiếng Việt + gõ thường
private const int BulkClassifyThreshold   = 64;   // > 64 ký tự → bulk thay vì typing
```

**Lý do chọn `BulkMaxCharsPerPacket = 8192`**:
- 1 ký tự UTF-16 ≤ 4 byte JSON-escape (worst case). 8192 × 4 ≈ 32KB / packet.
- TCP buffer + 4-byte length prefix chấp nhận thoải mái.
- Paste 1000 ký tự = 1 packet. Paste 50KB = 7 packet.
- Còn dư biên độ an toàn (không phải `int.MaxValue`).

**KHÔNG dùng `int.MaxValue`** vì:
- 1 op `text` 1MB → 1 transform OT phải scan hết → block lock per-doc → user khác đợi.
- Crash sender khi serialize lớn.

---

## 4. Phase A — Log và verify (BẮT BUỘC, KHÔNG được skip)

### Mục tiêu
Đo lường trước/sau, đồng thời xác định chính xác nguyên nhân vấn đề #3 (editor đơ — có 3 nghi ngờ).

### File sửa
`MarkTogether.Client/TypeRenderForm.cs` — chỉ thêm `Logger.Log(...)`, KHÔNG sửa logic.

### Điểm log thêm

| Vị trí | Log content |
|--------|-------------|
| Đầu `txtRawMarkdown_TextChanged` (sau `try {`) | `[Editor] TextChanged len={txtRawMarkdown.TextLength} suppress={_suppressOpTracking}` |
| `TrackRealtimeEditOps` sau khi tính `delta` | `[OT] Delta pos={delta.Position} del={delta.DeletedText?.Length ?? 0} ins={delta.InsertedText?.Length ?? 0}` |
| `QueueOperation` đầu hàm | `[OT] QueueOp type={opType} pos={position} len={text?.Length ?? 0} pendType={_pendingOpType} pendLen={_pendingOpText.Length}` |
| `FlushPendingEditOperation` đầu hàm (chỉ khi `_pendingOpType.HasValue`) | `[OT] Flush type={_pendingOpType} pos={_pendingOpStartPos} len={_pendingOpText.Length}` |
| `SendCurrentPendingChunk` sau khi enqueue | `[OT] Enqueue type={_pendingOpType} pos={_pendingOpStartPos} len={chunk.Length} q={_pasteChunkQueue.Count}` |
| `EnsurePasteSenderRunning` đầu | `[OT] EnsureSender hasTask={_pasteSendTask != null} done={(_pasteSendTask?.IsCompleted ?? true)} q={_pasteChunkQueue.Count}` |
| `PasteSenderLoop` trước Send | `[OT] SendChunk op={opChar} pos={pos} len={text.Length} rev={_clientRevision}` |
| `PasteSenderLoop` sau Increment rev | `[OT] SendChunk OK newRev={_clientRevision}` |
| `PasteSenderLoop` catch | `[OT] SendChunk FAIL {ex.GetType().Name}: {ex.Message} loggedIn={SocketClient.Instance.IsLoggedIn} q={_pasteChunkQueue.Count}` |
| `HandleOpBroadcast` trước `BeginInvoke` | `[OT] BroadcastEnter ops={p.ops?.Count ?? 0} type={p.opType} rev={p.clientResivion}` |
| `HandleOpBroadcast` cuối foreach apply | `[OT] BroadcastApplied ops={p.ops?.Count ?? 0} finalLen={txtRawMarkdown.TextLength}` |

### Reproduce + thu log

| Case | Hành động | Đếm |
|------|-----------|-----|
| L1 — IME tiếng Việt | A gõ `xin chaof` (Telex → `xin chào`) | Số `[OT] Enqueue` trên A + số `[OT] BroadcastEnter` trên B |
| L2 — Paste 200 ký tự | A Ctrl+V | Số `[OT] Enqueue` + `[OT] BroadcastEnter` |
| L3 — Xóa block 500 ký tự | A select + Delete | Số `[OT] Enqueue` + `[OT] BroadcastEnter` |
| L4 — Replace block | A select 100 ký tự + paste 100 ký tự | Số op Delete + Insert |
| L5 — 2 user gõ song song 30s | A + B gõ tiếng Việt | Quan sát `EnsureSender hasTask=true done=false q>0` lặp lại không |

**STOP sau Phase A** — gửi log cho người dùng review trước khi sang Phase B.

### Rủi ro
- Log nhiều → file lớn. **Mitigation**: KHÔNG log nội dung text, chỉ log length + position + type. Tổng độ dài dòng log ≤ 200 byte.

---

## 5. Phase B — `ClassifyEditCase` (kiến trúc xương sống)

### Mục tiêu
Tách logic phân loại edit thành một hàm rõ ràng để các phase sau (C–F) hook vào.

### File sửa
`MarkTogether.Client/TypeRenderForm.cs`

### Logic CŨ (line 577–614 — `TrackRealtimeEditOps`)

```csharp
private void TrackRealtimeEditOps(string newText)
{
    // tính delta
    var delta = ComputeTextDelta(oldText, newText);
    if (delta == null) { ... }

    if (!string.IsNullOrEmpty(delta.DeletedText) && !string.IsNullOrEmpty(delta.InsertedText))
    {
        FlushPendingEditOperation();
        QueueOperation(PendingOpType.Delete, delta.Position, delta.DeletedText);
        FlushPendingEditOperation();
        QueueOperation(PendingOpType.Insert, delta.Position, delta.InsertedText);
        FlushPendingEditOperation();
    }
    else if (!string.IsNullOrEmpty(delta.InsertedText))
        QueueOperation(PendingOpType.Insert, delta.Position, delta.InsertedText);
    else if (!string.IsNullOrEmpty(delta.DeletedText))
        QueueOperation(PendingOpType.Delete, delta.Position, delta.DeletedText);

    _lastMarkdownText = newText;
}
```

### Logic MỚI

Thêm enum + hàm `ClassifyEditCase` + dispatch:

```csharp
private enum EditCase
{
    None,
    Typing,        // delta nhỏ ≤ BulkClassifyThreshold ký tự
    BulkInsert,    // chỉ inserted > threshold, no deleted
    BulkDelete,    // chỉ deleted > threshold, no inserted
    ReplaceBlock   // cả deleted lẫn inserted (kể cả < threshold thì vẫn replace, nhưng nếu ≤ threshold + cả 2 < 32 → coi là Typing để gộp)
}

private static EditCase ClassifyEditCase(TextDelta delta)
{
    if (delta == null) return EditCase.None;
    int del = delta.DeletedText?.Length ?? 0;
    int ins = delta.InsertedText?.Length ?? 0;
    if (del == 0 && ins == 0) return EditCase.None;

    bool delBulk = del > BulkClassifyThreshold;
    bool insBulk = ins > BulkClassifyThreshold;

    if (del > 0 && ins > 0)
    {
        // Replace block: nếu cả 2 đều nhỏ (≤ threshold), vẫn coi là Typing
        // để pipeline cũ gộp (vd Telex 'a'→'á' = del 1 + ins 1).
        if (!delBulk && !insBulk) return EditCase.Typing;
        return EditCase.ReplaceBlock;
    }
    if (insBulk) return EditCase.BulkInsert;
    if (delBulk) return EditCase.BulkDelete;
    return EditCase.Typing;
}
```

Thay nội dung `TrackRealtimeEditOps` (giữ tên hàm) thành dispatch:

```csharp
private void TrackRealtimeEditOps(string newText)
{
    string oldText = _lastMarkdownText ?? string.Empty;
    if (newText == oldText) return;

    if (!_trackRealtimeOps || string.IsNullOrWhiteSpace(_docId) || !SocketClient.Instance.IsLoggedIn)
    {
        _lastMarkdownText = newText;
        return;
    }

    var delta = ComputeTextDelta(oldText, newText);
    var kase = ClassifyEditCase(delta);
    Logger.Log($"[OT] Classify case={kase} del={delta?.DeletedText?.Length ?? 0} ins={delta?.InsertedText?.Length ?? 0}");

    switch (kase)
    {
        case EditCase.None:
            break;

        case EditCase.Typing:
            // Pipeline typing cũ — gộp Delete+Insert nhỏ (giữ Flush giữa để không lẫn type)
            if ((delta.DeletedText?.Length ?? 0) > 0 && (delta.InsertedText?.Length ?? 0) > 0)
            {
                FlushPendingEditOperation();
                QueueOperation(PendingOpType.Delete, delta.Position, delta.DeletedText);
                FlushPendingEditOperation();
                QueueOperation(PendingOpType.Insert, delta.Position, delta.InsertedText);
                FlushPendingEditOperation();
            }
            else if ((delta.InsertedText?.Length ?? 0) > 0)
                QueueOperation(PendingOpType.Insert, delta.Position, delta.InsertedText);
            else if ((delta.DeletedText?.Length ?? 0) > 0)
                QueueOperation(PendingOpType.Delete, delta.Position, delta.DeletedText);
            break;

        case EditCase.BulkInsert:
            FlushPendingEditOperation();
            EnqueueBulkOp(PendingOpType.Insert, delta.Position, delta.InsertedText);
            break;

        case EditCase.BulkDelete:
            FlushPendingEditOperation();
            EnqueueBulkOp(PendingOpType.Delete, delta.Position, delta.DeletedText);
            break;

        case EditCase.ReplaceBlock:
            FlushPendingEditOperation();
            // Delete trước (nguyên block) rồi Insert (nguyên block).
            // Cả 2 đều dùng pipeline bulk để không bị chunk 32.
            EnqueueBulkOp(PendingOpType.Delete, delta.Position, delta.DeletedText);
            EnqueueBulkOp(PendingOpType.Insert, delta.Position, delta.InsertedText);
            break;
    }

    _lastMarkdownText = newText;
}
```

### Hàm `EnqueueBulkOp` (mới)

```csharp
private void EnqueueBulkOp(PendingOpType opType, int basePos, string text)
{
    if (string.IsNullOrEmpty(text)) return;

    // Chia thành các chunk BulkMaxCharsPerPacket (≤8192 ký tự / packet).
    // KHÔNG chia 32. Mỗi chunk = 1 op = 1 packet.
    lock (_pasteLock)
    {
        if (opType == PendingOpType.Insert)
        {
            int insertPos = basePos;
            for (int i = 0; i < text.Length;)
            {
                int size = SafeChunkLength(text, i, BulkMaxCharsPerPacket);
                string chunk = text.Substring(i, size);
                _pasteChunkQueue.Enqueue($"I|{insertPos}|{chunk}");
                insertPos += size;
                i += size;
            }
        }
        else // Delete
        {
            // Delete luôn xóa ở basePos. Khi chunk 8192:
            //   chunk1 = 8192 ký tự đầu of deletedText, xóa tại basePos
            //   chunk2 = 8192 ký tự kế, vẫn xóa tại basePos (vì sau khi xóa chunk1, ký tự kế dịch về basePos)
            for (int i = 0; i < text.Length;)
            {
                int size = SafeChunkLength(text, i, BulkMaxCharsPerPacket);
                string chunk = text.Substring(i, size);
                _pasteChunkQueue.Enqueue($"D|{basePos}|{chunk}");
                i += size;
            }
        }
    }
    Logger.Log($"[OT] BulkEnqueue type={opType} pos={basePos} totalLen={text.Length} chunks={(text.Length + BulkMaxCharsPerPacket - 1) / BulkMaxCharsPerPacket}");
    EnsurePasteSenderRunning();
}
```

### Rủi ro Phase B
- Quy tắc `delta.DeletedText > 0 && delta.InsertedText > 0` nhỏ → vẫn coi là Typing: cần thiết để IME tiếng Việt (`a→á` = del 1 + ins 1) đi đúng nhánh typing với debounce.
- Nếu delta tính sai (prefix/suffix collision với content có pattern lặp) → vẫn dùng `ComputeTextDelta` hiện tại, KHÔNG sửa hàm này.

### Test
- Gõ `hello` → Classify = Typing (5 lần, mỗi lần ins ≤ 1).
- Gõ `chaof` → 4 Typing events (cả del+ins ≤ 1 nhưng có replace dấu).
- Paste 100 ký tự → Classify = BulkInsert (1 lần).
- Delete 100 ký tự → Classify = BulkDelete.
- Select 100 + paste 100 → Classify = ReplaceBlock.

---

## 6. Phase C — Typing debounce cho tiếng Việt (case Typing)

### Mục tiêu
Gom các intermediate TextChanged của IME tiếng Việt thành 1 delta cuối. Áp dụng cho cả gõ thường (gõ nhanh cũng gom được).

### File sửa
`MarkTogether.Client/TypeRenderForm.cs`

### Thêm field (cạnh các Timer field, line 30–32)

```csharp
private readonly Timer _typingDebounceTimer;
private string _typingBaselineText; // text snapshot lúc bắt đầu burst typing
```

### Init trong constructor (sau `_draftTimer.Tick += ...` line 152)

```csharp
_typingDebounceTimer = new Timer { Interval = TypingDebounceMs };
_typingDebounceTimer.Tick += TypingDebounceTimer_Tick;
```

### Sửa `txtRawMarkdown_TextChanged` (line 482–517)

Logic CŨ:
```csharp
if (!_suppressOpTracking)
    TrackRealtimeEditOps(txtRawMarkdown.Text ?? string.Empty);
else
    _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;
```

Logic MỚI:
```csharp
if (!_suppressOpTracking)
{
    // Snapshot baseline ở event ĐẦU của burst để debounce timer tính delta đúng
    if (_typingBaselineText == null)
        _typingBaselineText = _lastMarkdownText ?? string.Empty;

    // Peek size của delta hiện tại — nếu rất lớn (paste qua TextChanged, drag-drop),
    // flush ngay không cần đợi debounce.
    string currentText = txtRawMarkdown.Text ?? string.Empty;
    int diff = Math.Abs(currentText.Length - _typingBaselineText.Length);
    if (diff > BulkClassifyThreshold * 2) // 128 ký tự
    {
        // Bulk: không debounce
        _typingDebounceTimer.Stop();
        TypingDebounceTimer_Tick(this, EventArgs.Empty);
    }
    else
    {
        // Typing nhỏ: debounce
        _typingDebounceTimer.Stop();
        _typingDebounceTimer.Start();
    }
}
else
{
    // Đang apply broadcast hoặc paste — KHÔNG cập nhật _lastMarkdownText ở đây.
    // Path apply sẽ tự cập nhật 1 lần ở cuối (tối ưu trong Phase H).
}
```

### Thêm `TypingDebounceTimer_Tick`

```csharp
private void TypingDebounceTimer_Tick(object sender, EventArgs e)
{
    _typingDebounceTimer.Stop();
    try
    {
        if (_typingBaselineText == null) return;
        string baseline = _typingBaselineText;
        _typingBaselineText = null;

        string current = txtRawMarkdown.Text ?? string.Empty;
        if (baseline == current) { _lastMarkdownText = current; return; }

        // Tạm thời gán _lastMarkdownText = baseline để TrackRealtimeEditOps
        // tính delta giữa baseline (snapshot lúc bắt đầu burst) và current.
        _lastMarkdownText = baseline;
        TrackRealtimeEditOps(current);
    }
    catch (Exception ex)
    {
        Logger.Log($"[OT] TypingDebounce exception: {ex.GetType().Name}: {ex.Message}");
    }
}
```

### Reset baseline khi broadcast về (line 791, trong `HandleOpBroadcast` BeginInvoke)

Trước `_suppressOpTracking = true;` thêm:

```csharp
// Broadcast đã thay đổi text → baseline không còn dùng được nữa
_typingDebounceTimer.Stop();
_typingBaselineText = null;
```

`FlushPendingEditOperation()` ở line 791 giữ nguyên.

### Flush khi đóng form (line ~1712 `OnFormClosing`)

Trước `FlushPendingEditOperation()` (line 1715):

```csharp
if (_typingDebounceTimer != null && _typingDebounceTimer.Enabled)
{
    _typingDebounceTimer.Stop();
    try { TypingDebounceTimer_Tick(this, EventArgs.Empty); } catch { }
}
```

Trong `OnFormClosed` dispose timer (cạnh `_draftTimer.Dispose();` line 1692):

```csharp
_typingDebounceTimer.Tick -= TypingDebounceTimer_Tick;
_typingDebounceTimer.Dispose();
```

### Tương tác với các path khác

- `HandleDocReloadBroadcast` (line 871): trong `BeginInvoke`, trước `_suppressOpTracking = true;` (line 881) thêm reset baseline tương tự.
- `RestoreVersion` / AI undo / template apply (line 1054–1057): các chỗ `_suppressOpTracking = true; txtRawMarkdown.Text = ...; _lastMarkdownText = ...; _suppressOpTracking = false;` cần thêm `_typingBaselineText = null;` ở giữa.

### Rủi ro Phase C
- User gõ liên tục > 8 ký tự/giây không nghỉ ≥ 120ms: debounce có thể trì hoãn lâu. **Mitigation**: nhánh peek `diff > 128` flush ngay (đã có ở trên).
- Caret jump do click chuột trong khi debounce: vẫn OK vì delta dựa content, không dựa caret.
- Form đóng giữa burst: đã có `OnFormClosing` flush.

### Test
- Gõ `chaof` → 1 op `Insert(pos, "chào")` sau 120ms thay vì 4 cặp insert/delete.
- Số `[OT] BroadcastEnter` trên B = số ký tự logical (4) chứ không phải 6–10.

## 7. Phase D — BulkInsert (case Paste dài)

### Mục tiêu
Paste 1000 ký tự → 1 packet `OP_INSERT` với 1 op `text.Length = 1000` (hoặc tối đa 8192) thay vì 32 packet × 32 ký tự.

### File sửa
`MarkTogether.Client/TypeRenderForm.cs`

### Đường vào 1 — paste qua Ctrl+V (`PerformFastPaste` line 1801)

Logic CŨ — line 1847:
```csharp
EnqueuePasteOperations(selStart, selectedText, pasted);
EnsurePasteSenderRunning();
```

Logic MỚI:

```csharp
// Nếu replace block (có selectedText) → ReplaceBlock pipeline.
// Nếu insert thuần → BulkInsert pipeline.
if (!string.IsNullOrEmpty(selectedText))
{
    // Replace: gửi 1 Delete bulk + 1 Insert bulk
    EnqueueBulkOp(PendingOpType.Delete, selStart, selectedText);
    EnqueueBulkOp(PendingOpType.Insert, selStart, pasted);
}
else
{
    EnqueueBulkOp(PendingOpType.Insert, selStart, pasted);
}
// EnsurePasteSenderRunning đã được gọi bên trong EnqueueBulkOp
```

**Xóa hàm `EnqueuePasteOperations`** (line 1851–1876) — đã được thay thế bằng `EnqueueBulkOp`. Hoặc giữ lại nhưng đánh dấu deprecated. Đề xuất xoá để không lẫn.

### Đường vào 2 — drag-drop / set Text programmatic (qua TextChanged)

Đã được Phase B (`ClassifyEditCase = BulkInsert`) bắt rồi → tự gọi `EnqueueBulkOp`.

### Sửa `PasteSenderLoop` để hỗ trợ ops không bị giới hạn 32

Hiện loop parse entry như "I|pos|text" và gửi 1 op / packet. Logic giữ nguyên — chỉ cần đảm bảo server chấp nhận text dài (Phase G).

**Đọc thêm**: trong `PasteSenderLoop` line 1936–1944, mỗi entry → `SendInsertOps/SendDeleteOps` với `ops = [1 EditOpItem]`. Đúng rồi. KHÔNG cần thay đổi loop.

### Rủi ro Phase D
- Server CHƯA chấp nhận `charCount > 32` → packet sẽ bị reject. **PHẢI làm Phase G trước, deploy server trước client.**
- Packet JSON 32KB: nếu `Network/SocketClient.cs` đang dùng buffer < 32KB ở chỗ nào → drop. **Kiểm tra**: đọc `PacketHelper.cs` — 4-byte length prefix tức max 2^31 byte, OK.

### Test
- Paste 200 ký tự → 1 packet `OP_INSERT` với `ops=[{pos,text(200)}]`.
- Paste 5000 ký tự → 1 packet (vì 5000 ≤ 8192).
- Paste 16384 ký tự → 2 packet (chia ở 8192).

---

## 8. Phase E — BulkDelete (case Xóa đoạn dài)

### Mục tiêu
Select 500 ký tự + Delete → 1 packet `OP_DELETE` thay vì 16 packet × 32 ký tự.

### File sửa
`MarkTogether.Client/TypeRenderForm.cs`

### Đường vào — qua `TextChanged`

Phase B đã phân loại Delete > threshold → `EditCase.BulkDelete` → `EnqueueBulkOp(Delete, ...)`. KHÔNG có code mới ngoài Phase B/D.

### Lưu ý đặc thù Delete

`EnqueueBulkOp` cho Delete enqueue nhiều chunk ở **cùng `basePos`** (xem ghi chú trong Phase B). Lý do:
- Sau khi chunk 1 (xóa 8192 ký tự đầu) được apply tại `basePos`, các ký tự tiếp theo dịch về `basePos`.
- Chunk 2 (xóa 8192 ký tự tiếp) cũng tại `basePos`.

Logic này giống pipeline cũ `EnqueuePasteOperations` (delete cùng basePos). KHÔNG đổi.

### Test
- Select 500 ký tự + Delete → 1 packet `OP_DELETE`, `text` chứa 500 ký tự deleted, server transform + apply 1 lần.
- Select 16000 ký tự + Delete → 2 packet (8192 + 7808 chia tại basePos).

---

## 9. Phase F — Replace block

### Mục tiêu
Select block + paste / type → tối đa 1 `OP_DELETE` + 1 `OP_INSERT` (mỗi cái có thể nhiều chunk nếu > 8192).

### File sửa
`MarkTogether.Client/TypeRenderForm.cs`

### Đường vào 1 — Ctrl+V vào selection (`PerformFastPaste`)
Đã xử lý ở Phase D (nhánh `if (!string.IsNullOrEmpty(selectedText))`).

### Đường vào 2 — Select + type ký tự / paste text (qua TextChanged)
Phase B đã phân loại `EditCase.ReplaceBlock` → gọi `EnqueueBulkOp(Delete, ...)` rồi `EnqueueBulkOp(Insert, ...)`.

### Đường vào 3 — Select nhỏ + type 1 ký tự (vd thay 5 ký tự bằng 1 ký tự)
Phase B classify thấy `del=5, ins=1, cả 2 ≤ threshold` → coi là **Typing** (gộp Telex `a→á`). Pipeline typing cũ xử lý bằng `Delete + Insert` riêng (line 596–602).

### Quan trọng — Thứ tự gửi Delete trước Insert

`EnqueueBulkOp` enqueue vào `_pasteChunkQueue` theo thứ tự FIFO. Loop send tuần tự → Delete tới server trước, Insert sau. Server apply tuần tự → state đúng.

**Race với broadcast**: nếu user khác broadcast op vào giữa Delete và Insert của ReplaceBlock, OT transform sẽ xử lý đúng (`OTEngine` đã sửa đợt 1).

### Rủi ro Phase F
- Nếu Delete fail nhưng Insert thành công → state sai (text bị duplicate). **Mitigation**: trong `PasteSenderLoop` catch (line 1946) — KHÔNG clear queue trừ khi `IsLoggedIn=false`. Giữ logic hiện tại, không thay đổi.
- Nếu user undo (Ctrl+Z) trong giữa: WinForms TextBox tự undo, text rollback → TextChanged fire → delta mới sẽ bắt được rollback và gửi op ngược lại. OK.

### Test
- Select 100 ký tự + paste 200 ký tự → 1 packet Delete (text 100) + 1 packet Insert (text 200). Tổng 2 packet thay vì 32 packet × 32 ký tự.

---

## 10. Phase G — Server accept bulk packet

### Mục tiêu
Cho phép `OP_INSERT/OP_DELETE` chứa op có `text.Length` lớn (≤ BulkMaxCharsPerPacket = 8192).

### File sửa
`MarkTogether.Server/Network/ClientHandler.cs`

### Logic CŨ — line 1143–1148

```csharp
int charCount = (ops ?? new List<EditOpItem>()).Sum(op => op?.text?.Length ?? 0);
if (charCount > 32)
{
    ReplyError(packet, $"OP_{opType.ToUpper()} chỉ được chứa tối đa 32 ký tự.");
    return;
}
```

### Logic MỚI

```csharp
// Đồng bộ với client BulkMaxCharsPerPacket = 8192 (xem TypeRenderForm.cs).
// Vẫn giới hạn để chống abuse / OOM.
const int BulkMaxCharsPerPacket = 8192;

int charCount = (ops ?? new List<EditOpItem>()).Sum(op => op?.text?.Length ?? 0);
if (charCount > BulkMaxCharsPerPacket)
{
    ReplyError(packet, $"OP_{opType.ToUpper()} chỉ được chứa tối đa {BulkMaxCharsPerPacket} ký tự.");
    return;
}

// Giới hạn số op trong 1 packet để tránh abuse
const int MaxOpsPerPacket = 64;
if ((ops?.Count ?? 0) > MaxOpsPerPacket)
{
    ReplyError(packet, $"OP_{opType.ToUpper()} chỉ được chứa tối đa {MaxOpsPerPacket} op.");
    return;
}
```

### Batch broadcast (giữ từ Đợt 1's spirit)

Server hiện loop broadcast 1 op / packet (line 1174–1186). Thay bằng 1 broadcast với toàn bộ `transformedOps`:

Logic CŨ:
```csharp
foreach (var tOp in transformedOps)
{
    var broadcast = new Payload_OP_BROADCAST
    {
        docID = docId,
        clientResivion = state.ServerRevision,
        userID = currentUserId,
        username = _username,
        opType = opType,
        ops = new List<EditOpItem> { tOp }
    };
    SessionManager.BroadcastToRoom(docId, Packet.Create(MessageType.OP_BROADCAST, broadcast), exclude: this);
}
```

Logic MỚI:
```csharp
if (transformedOps.Count > 0)
{
    var broadcast = new Payload_OP_BROADCAST
    {
        docID = docId,
        clientResivion = state.ServerRevision,
        userID = currentUserId,
        username = _username,
        opType = opType,
        ops = transformedOps
    };
    SessionManager.BroadcastToRoom(docId, Packet.Create(MessageType.OP_BROADCAST, broadcast), exclude: this);
}
```

### Thứ tự deploy
1. **Deploy server trước.** Server mới (limit 8192) chấp nhận cả client cũ (gửi ≤ 32) lẫn client mới (≤ 8192).
2. **Sau đó deploy client.** Client cũ vẫn chạy được trên server mới.
3. Nếu rollback: rollback client trước, sau đó server.

### Rủi ro Phase G
- Server transform/apply 1 op `text.Length=8192`: `OTEngine.Transform*` chỉ xử lý pos + length, KHÔNG scan text content → O(serverOps × 1). Repository INSERT 1 row với `text=8192 ký tự` → DB column phải support. **PHẢI kiểm tra schema**:
  - Đọc `MarkTogether.Server/Database/Scripts/schema_init.sql` xem `document_operations.text` có TEXT type không. Nếu là `VARCHAR(50)` thì sẽ overflow.
- Broadcast 8192 ký tự × 64 op = 512KB JSON. Đo bằng log size lớn nhất từng gặp. Nếu OK với 32KB / 1 broadcast, scale tuyến tính tới 1MB nên cần để `MaxOpsPerPacket = 64` đủ.

### Test
- Client 32 (cũ) → server mới: vẫn gửi OK, server log charCount ≤ 32.
- Client 8192 (mới) → server mới: gửi `OP_INSERT` 1000 ký tự, server log charCount=1000, apply OK.
- Client 9000 (test boundary) → reject với message "tối đa 8192 ký tự".

---

## 11. Phase H — Optimize remote apply (`HandleOpBroadcast`)

### Mục tiêu
Giảm số lần đọc `txtRawMarkdown.Text` (mỗi lần là O(N) clone). Giảm `BeginInvoke` flood khi broadcast chứa nhiều op.

### File sửa
`MarkTogether.Client/TypeRenderForm.cs`

### Logic CŨ — line 786–846 (`HandleOpBroadcast` BeginInvoke)

```csharp
BeginInvoke((Action)(() =>
{
    try
    {
        FlushPendingEditOperation();
        _suppressOpTracking = true;
        int caret = txtRawMarkdown.SelectionStart;

        foreach (var op in p.ops ?? Enumerable.Empty<EditOpItem>())
        {
            if (op == null) continue;
            string current = txtRawMarkdown.Text ?? string.Empty;  // ← O(N) clone mỗi op
            int pos = Math.Max(0, Math.Min(op.pos, current.Length));
            string text = op.text ?? "";
            if (p.opType == "insert") { ... }
            else { ... }
        }

        int finalLen = txtRawMarkdown.TextLength;
        txtRawMarkdown.SelectionStart = ...;
        _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;  // ← O(N) clone
        ...
    }
}
```

### Logic MỚI

```csharp
BeginInvoke((Action)(() =>
{
    try
    {
        // Reset typing baseline trước khi áp broadcast
        _typingDebounceTimer.Stop();
        _typingBaselineText = null;

        FlushPendingEditOperation();
        _suppressOpTracking = true;
        int caret = txtRawMarkdown.SelectionStart;

        // Tối ưu: thay vì đọc txtRawMarkdown.Text mỗi op (O(N) clone),
        // dùng TextLength (rẻ, P/Invoke GetWindowTextLength) và tự maintain.
        int currentLen = txtRawMarkdown.TextLength;

        foreach (var op in p.ops ?? Enumerable.Empty<EditOpItem>())
        {
            if (op == null) continue;
            int pos = Math.Max(0, Math.Min(op.pos, currentLen));
            string text = op.text ?? "";
            if (p.opType == "insert")
            {
                txtRawMarkdown.SelectionStart = pos;
                txtRawMarkdown.SelectionLength = 0;
                txtRawMarkdown.SelectedText = text;
                currentLen += text.Length;
                if (caret >= pos) caret += text.Length;
            }
            else
            {
                int len = Math.Min(text.Length, currentLen - pos);
                if (len > 0)
                {
                    txtRawMarkdown.SelectionStart = pos;
                    txtRawMarkdown.SelectionLength = len;
                    txtRawMarkdown.SelectedText = "";
                    currentLen -= len;
                    if (caret > pos) caret -= Math.Min(len, caret - pos);
                }
            }
        }

        txtRawMarkdown.SelectionStart = Math.Max(0, Math.Min(caret, currentLen));
        txtRawMarkdown.SelectionLength = 0;
        // Đọc Text 1 lần duy nhất ở cuối (O(N) — không tránh được vì cần sync baseline)
        _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;

        Logger.Log($"[OT] BroadcastApplied ops={p.ops?.Count ?? 0} finalLen={currentLen}");

        // Monotonic revision update (giữ từ đợt 1)
        int broadcastRev = p.clientResivion;
        int observedRev;
        while (broadcastRev > (observedRev = System.Threading.Interlocked.CompareExchange(ref _clientRevision, 0, 0)))
        {
            if (System.Threading.Interlocked.CompareExchange(ref _clientRevision, broadcastRev, observedRev) == observedRev)
                break;
        }
    }
    catch (Exception ex)
    {
        Logger.Log($"[OT] Broadcast apply failed: {ex.GetType().Name}: {ex.Message}");
    }
    finally
    {
        _suppressOpTracking = false;
    }
}));
```

### Sửa `txtRawMarkdown_TextChanged` để KHÔNG đọc Text khi suppress

Đợt 1 hiện code (line 487–494) ở nhánh `_suppressOpTracking=true` vẫn `_lastMarkdownText = txtRawMarkdown.Text` mỗi event → O(N) per SelectedText assignment. Bỏ:

```csharp
if (!_suppressOpTracking)
{
    // ... debounce như Phase C ...
}
// else: KHÔNG làm gì. _lastMarkdownText sẽ được sync 1 lần ở cuối HandleOpBroadcast.
```

**Lưu ý phụ**: nhánh check `_hasUnsavedChanges` (line 496–499) và draftTimer (line 501–506) trước đây gắn vào `!_suppressOpTracking` — giữ nguyên.

### Rủi ro Phase H
- Nếu `currentLen` track tay sai (vd có CRLF normalize ngầm trong `SelectedText =`) → finalLen mismatch. **Mitigation**: ở cuối loop, đối chiếu `txtRawMarkdown.TextLength` thực tế với `currentLen`; nếu lệch, log warning. KHÔNG fix tự động.
- Các nhánh khác cũng `_suppressOpTracking = true` (HandleDocReloadBroadcast line 881, RestoreVersion line 1054…) đang dựa vào `_lastMarkdownText = txtRawMarkdown.Text` ở TextChanged → cần tự gán sau khi `_suppressOpTracking = false`. Đợt 1 code đã gán đúng (line 883, 1056) — không phá.

### Test
- Broadcast 32 op (vd remote paste 1000 ký tự sau Phase D = 1 op duy nhất; nhưng test backward với client cũ paste 32 chunk): chạy `[OT] BroadcastApplied` log 1 lần, finalLen đúng.

---

## 12. Phase I — Test bắt buộc

| Case | Hành động | Kỳ vọng (sau đợt 2) |
|------|-----------|---------------------|
| T1 — Gõ tiếng Anh thường | A gõ `hello world` | B nhận đầy đủ; mỗi ký tự 1 packet trong < 200ms; KHÔNG có flicker |
| T2 — Gõ Telex | A gõ `xin chaof` → `xin chào` | B nhận thẳng `xin chào`, không thấy `xin chao` trung gian; ≤ 5 packet |
| T3 — Gõ VNI | A gõ `cha2o` → `cháo` | B nhận thẳng `cháo`; không nhấp nháy |
| T4 — Telex dấu nặng chuyển hàng | A gõ `aw` → `ă`, `ow` → `ơ` | B nhận `ăơ` đúng, không có ký tự trung gian |
| T5 — Paste 200 ký tự | A Ctrl+V 200 ký tự | **1 packet** `OP_INSERT`; B nhận 1 BroadcastEnter; apply < 100ms |
| T6 — Paste 5000 ký tự | A Ctrl+V 5000 ký tự | **1 packet** (≤ 8192); B nhận 1 BroadcastEnter |
| T7 — Paste 20000 ký tự | A Ctrl+V 20000 ký tự | **3 packet** (chia 8192+8192+3616); B nhận 3 BroadcastEnter |
| T8 — Xóa block 500 ký tự | A select + Delete | **1 packet** `OP_DELETE`; B text thu lại đúng |
| T9 — Xóa block 10000 ký tự | A select + Delete | **2 packet** (8192+1808); B đúng |
| T10 — Replace 100 → 200 | A select 100 + paste 200 | **2 packet** (1 Delete 100 + 1 Insert 200); B hội tụ đúng |
| T11 — Replace 5000 → 10 | A select 5000 + paste 10 | **2 packet** (1 Delete 5000 + 1 Insert 10) |
| T12 — Replace 100 → 100 ký tự kế tiếp | Select 100 + type chữ thường thay thế | Nếu type 1 ký tự đầu tiên: Typing (del 100 ngắn, ins 1) — vẫn nên là Typing mặc dù del=100>threshold. **Phải verify Classify rule**: `del 100 + ins 1 = ReplaceBlock` vì `del > threshold`. OK. → 2 packet. |
| T13 — 2 user gõ song song tiếng Việt 30s | A + B đều gõ Telex | Cả 2 responsive, không freeze, text hội tụ |
| T14 — Đóng form giữa burst | A gõ + Close ngay | Op cuối được gửi (TypingDebounce flush trong OnFormClosing) |
| T15 — Backward client cũ + server mới | Client `MaxCharsPerPacket=32` (cũ) + server mới | Vẫn hoạt động, chỉ là chunk 32 (chậm hơn) |

**Quy trình**: Chạy thật trên 2 client (1 máy 2 instance được). Quan sát log + WebView preview.

---

## 13. Quy tắc nghiêm ngặt

❌ KHÔNG:
- Đổi `txtRawMarkdown` từ `TextBox` sang control khác.
- Refactor pipeline OT khác với spec (giữ `TextChanged → debounce → ClassifyEditCase → dispatch`).
- Thêm async/await vào `txtRawMarkdown_TextChanged`.
- Sửa `OTEngine.cs`, `DocumentState.cs`, `DocumentStateManager.cs` (đợt 1 đã sửa).
- Sửa `Packet.cs` / `PacketHelper.cs` / Gateway / protocol.
- Đặt `BulkMaxCharsPerPacket > 16384` hoặc unlimited.
- Implement reconnect / state resync (ngoài phạm vi).
- Comment dài, docstring nhiều dòng.

✅ Bắt buộc:
- Tận dụng protocol hiện có: `ops = List<EditOpItem>`.
- Mọi code mới có `try/catch + Logger.Log` để không crash UI thread.
- Log metadata, KHÔNG log nội dung text.
- Build sạch (0 error). Warning cũ giữ nguyên.
- Test thật 2 client.

---

## 14. Cập nhật tài liệu sau khi implement

### 14.1. `DOCUMENTATION.md`
- Mục 7.9: bổ sung pipeline mới `TextChanged → debounce → ClassifyEditCase → {Typing|BulkInsert|BulkDelete|ReplaceBlock}`. Ghi rõ `TypingMaxCharsPerPacket = 32`, `BulkMaxCharsPerPacket = 8192`, `TypingDebounceMs = 120`.
- Mục 10.3: server batch broadcast `transformedOps` trong 1 `OP_BROADCAST`; giới hạn `MaxOpsPerPacket = 64`, `BulkMaxCharsPerPacket = 8192`.
- Phụ lục OT: IME limitation (Unikey/EVKey không dùng WM_IME — dùng debounce thay composition event).

### 14.2. `OT_FIX_PLAN.md`
Thêm note cuối:
> ## Đợt 2 — Xem `OT_FIX_PLAN_V2.md` (2026-xx)
> Tách pipeline edit thành 4 case (Typing / BulkInsert / BulkDelete / ReplaceBlock). Thêm typing debounce 120ms cho IME tiếng Việt. Tăng giới hạn server từ 32 → 8192 ký tự / packet. Báo cáo: `OT_FIX_IMPLEMENTATION_REPORT_V2.md`.

### 14.3. Tạo `OT_FIX_IMPLEMENTATION_REPORT_V2.md`
Format giống đợt 1: phase đã làm, file đã đụng, build result, warning còn lại, ghi chú deploy (server trước client).

---

## 15. Thứ tự implement đề xuất

| Phase | Nội dung | Side | Phụ thuộc |
|-------|----------|------|-----------|
| A | Log + reproduce | Client | — |
| G | Server tăng limit + batch broadcast | Server | — (deploy đầu tiên) |
| B | `ClassifyEditCase` + `EnqueueBulkOp` | Client | G |
| C | Typing debounce | Client | B |
| D | BulkInsert (Ctrl+V + TextChanged) | Client | B, G |
| E | BulkDelete | Client | B, G |
| F | ReplaceBlock | Client | D, E |
| H | Optimize remote apply | Client | B, G |
| I | Test T1–T15 | Both | A–H |

**Lưu ý deploy**:
1. Build + deploy server (Phase G) trước.
2. Build + deploy client (Phase A, B, C, D, E, F, H) sau.
3. Nếu rollback: rollback client trước, server sau.

---

## 16. Tóm tắt thay đổi key (cheat sheet cho AI implement)

| File | Thay đổi |
|------|----------|
| `TypeRenderForm.cs` | Thêm enum `EditCase`, hàm `ClassifyEditCase`, `EnqueueBulkOp`. Thêm timer `_typingDebounceTimer` + handler. Sửa `txtRawMarkdown_TextChanged` (debounce). Sửa `TrackRealtimeEditOps` (dispatch theo case). Sửa `PerformFastPaste` (gọi `EnqueueBulkOp` thay `EnqueuePasteOperations`). Sửa `HandleOpBroadcast` (track `currentLen`, không đọc Text mỗi op). Xóa `EnqueuePasteOperations`. Thêm const `TypingMaxCharsPerPacket=32`, `BulkMaxCharsPerPacket=8192`, `TypingDebounceMs=120`, `BulkClassifyThreshold=64`. Reset baseline khi broadcast / reload / restore. Flush timer trong `OnFormClosing` + dispose trong `OnFormClosed`. |
| `ClientHandler.cs` | `ProcessRealtimeOp`: tăng `charCount > 32` thành `> 8192`, thêm `MaxOpsPerPacket = 64`. Batch broadcast `transformedOps` thành 1 packet thay vì foreach. |

---

## 17. Open questions (nếu phát sinh, hỏi người dùng TRƯỚC khi implement)

1. `document_operations.text` column trong PostgreSQL có TEXT type không? có TEXT type
2. `SocketClient.Request` timeout 5000ms có đủ cho packet 32KB qua VPS không? Nếu RTT cao + DB chậm khi INSERT row 8KB → có thể cần tăng `timeoutMs` lên 10000. tăng đi
3. Có cần giữ tương thích với client cũ (`MaxCharsPerPacket=5` legacy của đợt 1 tiền sửa) không? không 
