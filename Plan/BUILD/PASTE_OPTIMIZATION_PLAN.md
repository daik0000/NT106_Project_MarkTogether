# Kế hoạch fix: Paste text dài làm app WinForms bị đứng

> Phiên bản: 1.0 — 2026-05-21
> Đối tượng đọc: AI/Dev sẽ implement. Mọi bước đều có **file:dòng** cụ thể và **mã mẫu** copy-paste được.
> Mục tiêu: Giữ UI thread responsive khi paste, không vượt giới hạn 5 ký tự/op của server, không bể OT.

---

## 0. Tóm tắt nguyên nhân thực sự (đọc trước khi sửa)

DOCUMENTATION.md trong repo mô tả triệu chứng đúng nhưng **chẩn đoán nguyên nhân chưa hoàn toàn chính xác** — phải đọc kỹ phần này, vì nó quyết định kiến trúc fix:

### 0.1. Nguyên nhân CHÍNH (≈ 95% lag)

`MarkTogether.Client/Network/SocketClient.cs:352-370` — `SendInsertOps` / `SendDeleteOps` gọi `Request(...)` **đồng bộ** (block chờ ACK, timeout 5 s):

```csharp
public void SendInsertOps(string docId, int clientRevision, List<EditOpItem> ops)
{
    EnsureAuthenticated();
    var response = Request(MessageType.OP_INSERT, ..., timeoutMs: 5000);
    EnsureOkResponse(response, MessageType.OP_INSERT);
}
```

Hàm này hiện được gọi **trực tiếp trên UI thread** từ `TypeRenderForm.cs:679-684` (`SendCurrentPendingChunk`):

```csharp
if (_pendingOpType.Value == PendingOpType.Insert)
    SocketClient.Instance.SendInsertOps(_docId, _clientRevision, ops);
```

Paste 5000 ký tự → chunk 5 ký tự → ~1000 op → **1000 lần round-trip TCP đồng bộ trên UI thread**. Đây là lý do app "Not Responding".

### 0.2. Nguyên nhân PHỤ (≈ 5%)

- Preview render WebView2 đã có debounce 280 ms (`TypeRenderForm.cs:127, 469-471`) ⇒ paste **không** gây spam render. KHÔNG cần "thêm debounce paste" — đã có sẵn.
- `ComputeTextDelta` (`TypeRenderForm.cs:547-565`) chạy O(n) nhưng n ≤ vài MB là tức thì, không phải nguồn lag.

### 0.3. Ràng buộc CỨNG từ server — KHÔNG được vi phạm

`MarkTogether.Server/Network/ClientHandler.cs:1062-1067`:

```csharp
int charCount = (ops ?? new List<EditOpItem>()).Sum(op => op?.text?.Length ?? 0);
if (charCount > 5)
{
    ReplyError(packet, $"OP_{opType.ToUpper()} chỉ được chứa tối đa 5 ký tự.");
    return;
}
```

⇒ **Mỗi packet OP_INSERT/OP_DELETE bắt buộc ≤ 5 ký tự text.** Mọi đề xuất "gửi paste thành 1 op lớn" hoặc "chunk 200-500 ký tự" trong DOCUMENTATION.md **đều sai** với phiên bản server hiện tại. Hai lựa chọn:

- **Lựa chọn A (khuyến nghị):** Giữ giới hạn 5 ký tự, sửa **client-side đồng bộ ⇒ bất đồng bộ** (paste xử lý xong UI lập tức, op gửi nền). Đây là approach của plan này.
- **Lựa chọn B (mở rộng):** Nâng giới hạn server lên ví dụ 256 ký tự, đồng thời nâng `MaxCharsPerPacket` client lên cùng giá trị. Phải đảm bảo `OTEngine.Transform` xử lý op text > 1 ký tự đúng (đã đúng theo cách dùng `length` field). Cần test thêm; **nằm ngoài scope của plan này** — chỉ ghi chú nếu sau này muốn tăng throughput.

### 0.4. Ràng buộc OT (tránh phá đồng bộ)

- `_clientRevision` (`TypeRenderForm.cs:35, 683`) phải tăng **đơn điệu** theo thứ tự op gửi đi. Khi gửi paste nền, vẫn phải gửi **tuần tự** (không song song nhiều `Task.Run`) để giữ thứ tự op.
- Khi nhận `OP_BROADCAST` từ user khác **trong lúc đang flush paste**, `HandleOpBroadcast` (`TypeRenderForm.cs:691-735`) sẽ set `_suppressOpTracking=true`, áp op vào `txtRawMarkdown.Text`. Lúc này paste task đang chạy nền — phải xử lý: **toàn bộ paste phải được flush về `_lastMarkdownText` TRƯỚC khi nhường UI thread**, không thì broadcast remote sẽ thấy text "chưa có đoạn paste" và `ComputeTextDelta` ở các lần TextChanged kế tiếp sẽ tính sai.

⇒ **Kiến trúc đúng:** Insert text vào TextBox + cập nhật `_lastMarkdownText` xảy ra **đồng bộ trên UI thread** trong handler paste. Việc duy nhất chạy nền là **vòng lặp gửi op qua socket** (round-trip TCP chậm).

---

## 1. Tổng quan giải pháp

```
[User Ctrl+V trên txtRawMarkdown]
        │
        ▼
WM_PASTE / ProcessCmdKey intercept
        │
        ▼
HandlePasteAsync() — chạy trên UI thread:
  1) FlushPendingEditOperation()                  ← gửi nốt op đang gõ dở
  2) _suppressOpTracking = true; _isPasting = true
  3) Insert text vào TextBox (1 lần, đồng bộ)
  4) Cập nhật _lastMarkdownText
  5) Kích hoạt render preview (đã debounce 280 ms sẵn)
  6) _suppressOpTracking = false                  ← cho phép TextChanged tracking tiếp
  7) Tách paste thành chunks ≤ 5 ký tự → enqueue vào _pasteQueue
  8) Khởi động _pasteSendTask (single background task) nếu chưa chạy
        │
        ▼
_pasteSendTask (Task.Run nền, single-flight):
  while (_pasteQueue.TryDequeue(out chunk)) {
      SocketClient.Instance.SendInsertOps(...);    ← chậm nhưng KHÔNG ở UI thread
      _clientRevision++;                            ← tăng dưới lock
  }
  → khi rỗng: _isPasting = false
```

**Tính chất quan trọng:**
- Đoạn paste **xuất hiện ngay lập tức** trên editor — UI responsive ngay.
- Người dùng có thể gõ tiếp NGAY sau paste. `TrackRealtimeEditOps` vẫn đo delta đúng (vì `_lastMarkdownText` đã sync ở bước 4) và sinh op typing thường. Op typing này cũng đi qua `SendCurrentPendingChunk` đồng bộ — vẫn block UI thread như cũ với typing 1-5 ký tự, KHÔNG sao vì tần suất thấp. (Nếu muốn fix luôn cho typing → xem mục 7 "Optional follow-up".)
- Op paste và op typing **xen kẽ về thứ tự thời gian** nhưng `_clientRevision` vẫn tăng đơn điệu nếu tuần tự hoá qua một queue chung. ⇒ Plan này tách 2 đường gửi sẽ có race; xử lý ở §3.5.

---

## 2. File cần sửa

| File | Vai trò trong fix |
|------|-------------------|
| `MarkTogether.Client/TypeRenderForm.cs` | Toàn bộ logic mới (intercept paste, queue, background sender) |
| `MarkTogether.Client/TypeRenderForm.Designer.cs` | **KHÔNG sửa** — `txtRawMarkdown` vẫn là `TextBox`, không cần đổi sang `RichTextBox` |
| `MarkTogether.Server/Network/ClientHandler.cs` | **KHÔNG sửa** trong plan này (giữ ràng buộc 5 ký tự) |
| `MarkTogether.Client/Network/SocketClient.cs` | **KHÔNG sửa** — vẫn dùng `SendInsertOps`/`SendDeleteOps` đồng bộ |

---

## 3. Các bước implement (chi tiết)

### Bước 1 — Thêm field & infrastructure cho paste

**File:** `MarkTogether.Client/TypeRenderForm.cs`
**Vị trí:** Sau dòng 42 (`private readonly List<CommentDto> _comments = ...`)

Thêm:

```csharp
// ─── Paste handling ────────────────────────────────────────
private bool _isPasting;
private readonly object _pasteLock = new object();
private readonly System.Collections.Generic.Queue<string> _pasteChunkQueue
    = new System.Collections.Generic.Queue<string>();
private int _pasteChunkBasePos;       // vị trí bắt đầu của chunk kế tiếp trong queue
private System.Threading.Tasks.Task _pasteSendTask;

// Ngưỡng từ đó coi là paste: text clipboard >= ngưỡng này thì đi đường paste
// Ngắn hơn sẽ để typing logic xử lý như cũ (cảm giác mượt khi gõ phím dán nhanh)
private const int PasteThresholdChars = 20;
```

**Tại sao có `PasteThresholdChars`:**
Người dùng có thể Ctrl+V vài ký tự (URL ngắn, số). Trường hợp đó dán qua đường typing đồng bộ cũng không lag. Chỉ chuyển sang đường nền khi đáng (≥ 20 ký tự).

---

### Bước 2 — Intercept Ctrl+V và Shift+Insert qua `ProcessCmdKey`

**File:** `MarkTogether.Client/TypeRenderForm.cs`
**Vị trí:** Cuối class (trước dòng `}` đóng class — ngay sau `OnFormClosing`)

`TextBox` của WinForms không expose event paste. Hai cách: subclass control + override `WndProc` (xử lý `WM_PASTE = 0x0302`) — sạch hơn; hoặc override `ProcessCmdKey` trên form — đủ dùng vì textbox phải có focus thì Ctrl+V mới phát sinh. Plan này dùng `ProcessCmdKey` để không phải sửa Designer.cs.

Thêm:

```csharp
protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
{
    if (txtRawMarkdown != null
        && txtRawMarkdown.Focused
        && !txtRawMarkdown.ReadOnly
        && IsPasteKeyCombo(keyData))
    {
        try
        {
            if (TryHandlePaste())
                return true;   // đã nuốt phím — không cho default paste của TextBox chạy
        }
        catch (Exception ex)
        {
            // Fallback: log + để default xử lý để người dùng vẫn paste được
            Network.Logger.Log($"[Paste] Intercept failed: {ex.Message}");
        }
    }
    return base.ProcessCmdKey(ref msg, keyData);
}

private static bool IsPasteKeyCombo(Keys keyData)
{
    // Ctrl+V hoặc Shift+Insert
    return keyData == (Keys.Control | Keys.V)
        || keyData == (Keys.Shift   | Keys.Insert);
}

/// <summary>
/// Trả về true nếu đã xử lý paste (nuốt phím).
/// Trả về false nếu clipboard rỗng / không có text / paste ngắn → để default chạy.
/// </summary>
private bool TryHandlePaste()
{
    string clipboardText = null;
    try
    {
        if (Clipboard.ContainsText())
            clipboardText = Clipboard.GetText(TextDataFormat.UnicodeText);
    }
    catch
    {
        clipboardText = null;
    }

    if (string.IsNullOrEmpty(clipboardText))
        return false;   // clipboard rỗng / hình / file → để default thử

    // Chuẩn hoá: TextBox multiline trên Windows dùng "\r\n"
    clipboardText = NormalizePastedText(clipboardText);

    if (clipboardText.Length < PasteThresholdChars)
        return false;   // ngắn → để default paste (typing đồng bộ vẫn mượt)

    // Đường nhanh: tự insert + đẩy qua queue nền
    PerformFastPaste(clipboardText);
    return true;
}

private static string NormalizePastedText(string text)
{
    if (string.IsNullOrEmpty(text)) return text;
    // TextBox single-line không cho \n; multiline cần \r\n
    // Chuẩn hoá mọi line-ending về \r\n (Windows convention của WinForms TextBox)
    return System.Text.RegularExpressions.Regex.Replace(text, @"\r\n|\r|\n", "\r\n");
}
```

---

### Bước 3 — `PerformFastPaste`: insert text + enqueue chunks

**File:** `MarkTogether.Client/TypeRenderForm.cs`
**Vị trí:** Ngay sau `TryHandlePaste` (Bước 2)

```csharp
private void PerformFastPaste(string pasted)
{
    if (string.IsNullOrEmpty(pasted)) return;

    // 1) Flush mọi op typing đang pending TRƯỚC để giữ thứ tự revision
    //    (paste sẽ phát sinh op từ vị trí caret hiện tại, dựa trên _lastMarkdownText
    //     đã được cập nhật bởi các op typing trước đó.)
    FlushPendingEditOperation();

    // 2) Tính các vị trí dựa trên trạng thái TextBox hiện tại
    int selStart   = txtRawMarkdown.SelectionStart;
    int selLength  = txtRawMarkdown.SelectionLength;
    string currentText = txtRawMarkdown.Text ?? string.Empty;
    string selectedText = selLength > 0
        ? currentText.Substring(selStart, selLength)
        : string.Empty;

    // 3) Cập nhật TextBox: thay đoạn chọn bằng pasted text
    //    Bật cờ suppress để TextChanged KHÔNG tự sinh op từ delta này
    //    (vì ta sẽ tự sinh op ngay bên dưới và đẩy vào queue nền)
    _isPasting = true;
    _suppressOpTracking = true;
    try
    {
        // Dùng SelectedText để giữ undo-buffer tự nhiên của TextBox
        // và giảm flicker vs gán Text trực tiếp
        txtRawMarkdown.SelectedText = pasted;

        // Đặt caret sau đoạn vừa paste
        int newCaret = selStart + pasted.Length;
        txtRawMarkdown.SelectionStart  = newCaret;
        txtRawMarkdown.SelectionLength = 0;

        // Đồng bộ snapshot text để các lần TextChanged sau không sinh op trùng
        _lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;
        _hasUnsavedChanges = _lastMarkdownText != _lastSavedContent;
    }
    finally
    {
        _suppressOpTracking = false;
        // _isPasting giữ true cho tới khi sender task drain xong queue (xem Bước 5)
    }

    // 4) Kích preview render (đã debounce 280 ms, sẽ chỉ render 1 lần sau idle)
    _renderRequestVersion++;
    _renderDebounceTimer.Stop();
    _renderDebounceTimer.Start();

    // 5) Nếu không thực sự cần gửi op (single-user, chưa join room, không edit quyền)
    //    → thoát sau khi insert xong
    bool needSendOps = _trackRealtimeOps
        && !string.IsNullOrWhiteSpace(_docId)
        && SocketClient.Instance.IsLoggedIn
        && (_permission == "owner" || _permission == "editor");

    if (!needSendOps)
    {
        _isPasting = false;
        return;
    }

    // 6) Enqueue: trước hết là 1 op DELETE cho đoạn đang chọn (nếu có),
    //    tiếp đó là các chunk INSERT ≤ 5 ký tự cho text mới
    EnqueuePasteOperations(selStart, selectedText, pasted);

    // 7) Khởi động background sender (single-flight)
    EnsurePasteSenderRunning();
}
```

---

### Bước 4 — `EnqueuePasteOperations`: tách thành chunks ≤ 5 ký tự

**File:** `MarkTogether.Client/TypeRenderForm.cs`
**Vị trí:** Ngay sau `PerformFastPaste`

Format hàng đợi: mỗi entry là chuỗi có prefix encoding loại op + vị trí:
`"I|<pos>|<text>"` (insert) hoặc `"D|<pos>|<text>"` (delete). Đơn giản, không cần struct mới.

```csharp
private void EnqueuePasteOperations(int basePos, string replacedSelection, string pasted)
{
    lock (_pasteLock)
    {
        // 4a) Delete đoạn chọn cũ (nếu có) — server cũng chặn > 5 ký tự,
        //     nên cũng phải chunk ≤ 5
        if (!string.IsNullOrEmpty(replacedSelection))
        {
            // Delete xảy ra tại basePos. Mỗi packet vẫn xoá ≤ 5 ký tự tại CÙNG vị trí.
            // (Đối chiếu logic hiện có ở SendPendingChunksIfNeeded: delete không tăng pos.)
            for (int i = 0; i < replacedSelection.Length; i += MaxCharsPerPacket)
            {
                int size = Math.Min(MaxCharsPerPacket, replacedSelection.Length - i);
                string chunk = replacedSelection.Substring(i, size);
                _pasteChunkQueue.Enqueue($"D|{basePos}|{chunk}");
            }
        }

        // 4b) Insert text mới — chunk ≤ 5, vị trí tăng theo độ dài đã gửi
        int insertPos = basePos;
        for (int i = 0; i < pasted.Length; i += MaxCharsPerPacket)
        {
            int size = Math.Min(MaxCharsPerPacket, pasted.Length - i);
            string chunk = pasted.Substring(i, size);
            _pasteChunkQueue.Enqueue($"I|{insertPos}|{chunk}");
            insertPos += size;
        }
    }
}
```

**Lưu ý chunk boundary với surrogate pair (emoji / CJK đặc biệt):**
Trên `string` C#, `Substring(i, 5)` có thể cắt giữa **surrogate pair** (UTF-16) ⇒ ký tự hỏng khi server lưu DB. Khả năng xảy ra thấp nhưng bắt buộc xử lý nếu paste có emoji.

Thêm helper (đặt ở cùng region):

```csharp
/// <summary>
/// Cắt chunk ≤ maxLen ký tự nhưng KHÔNG chia đôi surrogate pair.
/// Trả về độ dài chunk thực sự (có thể là maxLen-1 nếu ký tự cuối là high surrogate).
/// </summary>
private static int SafeChunkLength(string text, int start, int maxLen)
{
    int len = Math.Min(maxLen, text.Length - start);
    if (len <= 0) return 0;
    // Nếu ký tự cuối chunk là high surrogate, lùi lại 1 để giữ pair nguyên
    if (len < text.Length - start
        && char.IsHighSurrogate(text[start + len - 1]))
    {
        len--;
    }
    return Math.Max(1, len);
}
```

Và thay thế `Math.Min(MaxCharsPerPacket, ...)` ở 4a / 4b bằng `SafeChunkLength(...)`:

```csharp
for (int i = 0; i < pasted.Length; )
{
    int size = SafeChunkLength(pasted, i, MaxCharsPerPacket);
    string chunk = pasted.Substring(i, size);
    _pasteChunkQueue.Enqueue($"I|{insertPos}|{chunk}");
    insertPos += size;
    i += size;
}
```

(Áp dụng tương tự cho block delete.)

---

### Bước 5 — Background sender (single-flight)

**File:** `MarkTogether.Client/TypeRenderForm.cs`
**Vị trí:** Sau `EnqueuePasteOperations`

```csharp
private void EnsurePasteSenderRunning()
{
    lock (_pasteLock)
    {
        if (_pasteSendTask != null && !_pasteSendTask.IsCompleted)
            return;   // đã chạy
        _pasteSendTask = System.Threading.Tasks.Task.Run((Action)PasteSenderLoop);
    }
}

private void PasteSenderLoop()
{
    try
    {
        while (true)
        {
            string entry;
            lock (_pasteLock)
            {
                if (_pasteChunkQueue.Count == 0)
                {
                    _isPasting = false;
                    return;
                }
                entry = _pasteChunkQueue.Dequeue();
            }

            // Parse "I|<pos>|<text>" / "D|<pos>|<text>"
            // text có thể chứa '|' nên dùng IndexOf để chỉ tách 2 separator đầu
            char opChar = entry[0];
            int firstBar  = 1;
            int secondBar = entry.IndexOf('|', firstBar + 1);
            if (secondBar < 0) continue;
            int pos = int.Parse(entry.Substring(firstBar + 1, secondBar - firstBar - 1));
            string text = entry.Substring(secondBar + 1);

            var ops = new System.Collections.Generic.List<EditOpItem>
            {
                new EditOpItem
                {
                    pos = pos,
                    text = text,
                    timestamp = DateTime.UtcNow
                }
            };

            try
            {
                int rev = _clientRevision;
                if (opChar == 'I')
                    SocketClient.Instance.SendInsertOps(_docId, rev, ops);
                else
                    SocketClient.Instance.SendDeleteOps(_docId, rev, ops);

                // Tăng revision sau khi server ACK (Request đã block đến khi nhận OK)
                System.Threading.Interlocked.Increment(ref _clientRevision);
            }
            catch (Exception ex)
            {
                // Không phá flow paste tiếp — log và bỏ qua chunk này
                // (server vẫn nhất quán vì các op kế tiếp sẽ map theo revision hiện tại)
                Network.Logger.Log($"[Paste] Send chunk failed: {ex.Message}");
            }
        }
    }
    finally
    {
        lock (_pasteLock)
        {
            _isPasting = false;
        }
    }
}
```

**Tại sao tuần tự, không song song:**
- Server transform OT dựa trên `clientRevision` mà client gửi kèm. Nếu chạy song song 2 chunk cùng `_clientRevision`, server sẽ transform sai (cả 2 op cùng giả định same base) → caret nhảy, mất ký tự.
- Tốc độ vẫn rất nhanh vì TCP localhost / LAN round-trip ≈ 1-5 ms / op. 5000 ký tự ÷ 5 = 1000 op × 2 ms ≈ 2 s — chấp nhận được, và quan trọng là **không trên UI thread**.

**Nâng cấp tốc độ tuỳ chọn (không bắt buộc):**
Có thể batch nhiều chunk vào 1 packet **nếu** sửa server giới hạn từ 5 → ví dụ 256. Xem §0.3.

---

### Bước 6 — Đồng bộ với typing path (chống race)

`txtRawMarkdown_TextChanged` (`TypeRenderForm.cs:453-472`) hiện sẽ chạy SAU khi `SelectedText = pasted` trong `PerformFastPaste`. Cờ `_suppressOpTracking = true` lúc đó sẽ vào nhánh else (dòng 459-462) chỉ set `_lastMarkdownText`. ⇒ KHÔNG sinh op trùng. **Đảm bảo trình tự:**

1. `_suppressOpTracking = true` SET *trước* khi `SelectedText = pasted` (đúng — `PerformFastPaste` Bước 3 đã làm vậy).
2. Sau khi gán xong, `TextChanged` event được fire **đồng bộ** ngay trong cùng call stack (WinForms TextBox), nên `_suppressOpTracking` vẫn = true lúc handler chạy. → set xong `_lastMarkdownText`, không vào `TrackRealtimeEditOps`. ✓

**Trường hợp user gõ tiếp ngay sau khi paste, trong khi sender task vẫn đang drain queue:**

- Op typing đi qua `SendCurrentPendingChunk` (UI thread, đồng bộ).
- Op paste đi qua `PasteSenderLoop` (background thread, đồng bộ TCP).
- Cả hai tăng `_clientRevision` — đã đổi sang `Interlocked.Increment` ở Bước 5 → an toàn.
- Vẫn có race về **thứ tự** giữa 2 đường: paste task lấy `_clientRevision = R`, gửi packet; typing UI cũng lấy `_clientRevision = R` (cùng giá trị nếu chưa kịp Interlocked), gửi đồng thời. Server sẽ thấy 2 op cùng base revision → transform OT cả 2 → vẫn đúng kết quả cuối (vì OT chính là để giải race này) **nhưng** caret typing có thể bị broadcast quay lại làm nhảy 1 chút. Mức độ ảnh hưởng nhỏ; chấp nhận được trong scope plan này.
- **Mitigation đơn giản (khuyến nghị):** Khi `_isPasting == true`, KHÔNG cho `SendCurrentPendingChunk` chạy — thay vào đó cũng đẩy vào `_pasteChunkQueue`. Thêm vào đầu hàm `SendCurrentPendingChunk`:

```csharp
if (_isPasting && _pendingOpType.HasValue)
{
    // Trì hoãn: chuyển sang queue để sender task gửi tuần tự
    lock (_pasteLock)
    {
        char op = _pendingOpType.Value == PendingOpType.Insert ? 'I' : 'D';
        _pasteChunkQueue.Enqueue($"{op}|{_pendingOpStartPos}|{chunk}");
    }
    EnsurePasteSenderRunning();
    return;
}
```

Đặt block này sau dòng `if (!_pendingOpType.HasValue || string.IsNullOrEmpty(chunk) || string.IsNullOrWhiteSpace(_docId)) return;` (`TypeRenderForm.cs:665`) và TRƯỚC khối tạo `ops` list.

Kết quả: trong giai đoạn paste, mọi op (cả paste lẫn typing) đi qua 1 đường tuần tự duy nhất → thứ tự revision hoàn toàn đúng.

---

### Bước 7 — Đồng bộ với remote `OP_BROADCAST` (rất quan trọng cho OT)

`HandleOpBroadcast` (`TypeRenderForm.cs:691-735`) áp op từ user khác vào `txtRawMarkdown.Text`. **Vấn đề:** Nếu remote op tới khi `_pasteChunkQueue` còn đang drain, các op paste sau đó dùng `pos` đã tính từ trạng thái TRƯỚC khi remote op áp dụng → server transform vẫn xử lý đúng (đây chính là việc của OT engine — transform clientOp với concurrent ops có revision > clientRev). **Không cần sửa gì ở `HandleOpBroadcast`** — OT đã lo.

Tuy nhiên, có một edge case: lúc `HandleOpBroadcast` chạy nó set `_suppressOpTracking = true`, gán `txtRawMarkdown.Text = current`, rồi set lại `_lastMarkdownText = current`. Nếu việc này xảy ra **đúng trong khoảnh khắc** giữa Bước 3.5 (set `_lastMarkdownText`) và Bước 3.6 (clear `_suppressOpTracking`) của `PerformFastPaste`, thì không có vấn đề. Nếu xảy ra **sau khi** đã clear `_suppressOpTracking` trong paste handler, thì cũng đã có cờ riêng của `HandleOpBroadcast`. ⇒ Logic độc lập, an toàn.

---

### Bước 8 — Cleanup khi đóng form

**File:** `MarkTogether.Client/TypeRenderForm.cs`
**Vị trí:** `OnFormClosing` (`TypeRenderForm.cs:1343-1363`)

Hiện tại `OnFormClosing` đã `FlushPendingEditOperation()`. Cần đảm bảo paste queue cũng được drain hoặc bỏ. Thêm vào ngay đầu `OnFormClosing`:

```csharp
// Đợi sender task drain queue (tối đa ~3 s) để không mất op paste
try
{
    var task = _pasteSendTask;
    if (task != null && !task.IsCompleted)
        task.Wait(TimeSpan.FromSeconds(3));
}
catch { /* ignore — vẫn tiếp tục đóng form */ }
```

Lý do timeout 3 s: paste 50000 ký tự / 5 = 10000 op × 2 ms = 20 s. Nếu user đóng form giữa chừng, đợi 3 s là đủ với paste hợp lý; quá hạn thì bỏ phần còn lại (server sẽ persist text qua `SaveDocument` cuối cùng trong `OnFormClosing` — text đầy đủ, chỉ là không có op history chi tiết).

---

### Bước 9 — Bonus: cũng intercept paste qua menu chuột phải

`System.Windows.Forms.TextBox` có built-in context menu (right-click) với mục Paste. Nó **không** đi qua `ProcessCmdKey`. Xử lý:

**Cách 1 (đơn giản):** Tắt context menu hệ thống bằng cách gán `txtRawMarkdown.ContextMenuStrip = new ContextMenuStrip();` (rỗng).

**Cách 2 (đầy đủ):** Subclass và override `WndProc` xử lý `WM_PASTE = 0x0302`. Tạo file mới `MarkTogether.Client/Controls/PasteAwareTextBox.cs`, đổi type trong Designer.cs. Vì plan này tránh sửa Designer, dùng Cách 1.

Thêm trong `TypeRenderForm_Shown` (`TypeRenderForm.cs:185`) hoặc ngay sau `InitializeComponent()` trong constructor:

```csharp
// Tắt context menu mặc định để paste qua chuột phải không lọt qua intercept
txtRawMarkdown.ContextMenu = new ContextMenu();  // .NET 4.7.2 dùng ContextMenu (legacy)
```

Trên .NET Framework 4.7.2 dùng `ContextMenu` (không phải `ContextMenuStrip`) cho TextBox cổ điển. Kiểm tra bằng IntelliSense; cả 2 đều set được, miễn là gán empty.

(Tuỳ chọn) Có thể build context menu custom với mục "Paste" gọi `TryHandlePaste()`. Ngoài scope nếu chỉ cần fix lag.

---

## 4. Cách test (acceptance criteria)

### Test case 1 — Paste 5000 ký tự

1. Mở Notepad, gõ/copy đoạn text 5000 ký tự bất kỳ (`a` × 5000).
2. Mở 2 client (A, B) cùng login, share doc, cùng vào editor.
3. Ở client A: click vào editor, Ctrl+V.
4. **Kết quả mong đợi:**
   - UI **không** "Not Responding".
   - Text 5000 ký tự **xuất hiện trên editor ngay lập tức** (< 100 ms).
   - Server console in `[OT] INSERT user=... text='...'` liên tục trong 2-5 s.
   - Client B nhận op dần dần, text cuối cùng khớp 100% với client A.
   - Markdown preview chỉ render **1 lần** sau khi gõ xong (≈ 280 ms idle).

### Test case 2 — Paste rồi gõ tiếp ngay

1. Paste đoạn 1000 ký tự.
2. Trong khi op chưa flush hết (queue còn ~100 op), gõ thêm vài phím ở cuối.
3. **Kết quả:** Phím gõ thêm cũng hiện ngay; sau khi sender drain xong, client B thấy đầy đủ paste + ký tự thêm, đúng thứ tự.

### Test case 3 — Paste ngắn

1. Copy 10 ký tự, Ctrl+V.
2. **Kết quả:** Đi đường default (vì < `PasteThresholdChars=20`), text hiện ngay, op gửi đồng bộ qua typing path. Không có khác biệt UX với typing thường.

### Test case 4 — Paste có emoji / CJK

1. Copy text có "Hello 👨‍👩‍👧 中文 hello".
2. Paste vào editor.
3. **Kết quả:** Text hiện đúng (không hỏng emoji/Hán tự); client B nhận đúng (kiểm tra trên DB `document_operations.text` không có `?` hay byte hỏng).

### Test case 5 — Paste rồi đóng form ngay

1. Paste 5000 ký tự.
2. Bấm "Trở về" / đóng form ngay sau ~500 ms.
3. **Kết quả:** Form đóng trong < 3 s; `documents.content` có đầy đủ 5000 ký tự (vì `OnFormClosing` gọi `SaveDocument`). Có thể thiếu một số op cuối trong `document_operations` — chấp nhận được.

### Test case 6 — Paste khi viewer (không có quyền edit)

1. Mở doc với quyền `viewer` (`txtRawMarkdown.ReadOnly == true`).
2. Ctrl+V.
3. **Kết quả:** `ProcessCmdKey` check `!txtRawMarkdown.ReadOnly` → false → fallthrough → default behavior → không có gì xảy ra (TextBox readonly không cho paste). Không lỗi.

### Test case 7 — Paste khi mất kết nối server

1. Tắt server.
2. Paste 1000 ký tự ở client.
3. **Kết quả:**
   - Text vẫn xuất hiện trên editor (vì insert là local).
   - Sender task bắn từng op, mỗi op throw exception (`Request` timeout 5 s × N op). Log nhiều dòng `[Paste] Send chunk failed: ...`.
   - **Vấn đề tiềm tàng:** mỗi failed send mất 5 s timeout × 200 chunks = 1000 s. ⇒ Cần short-circuit. Thêm trong `PasteSenderLoop` sau `catch`:

```csharp
catch (Exception ex)
{
    Network.Logger.Log($"[Paste] Send chunk failed: {ex.Message}");
    // Nếu là lỗi mạng (không phải lỗi nghiệp vụ), bỏ luôn các chunk còn lại
    if (!SocketClient.Instance.IsLoggedIn)
    {
        lock (_pasteLock) { _pasteChunkQueue.Clear(); }
        return;
    }
}
```

---

## 5. Checklist diff cuối cùng

- [ ] `TypeRenderForm.cs:42` (sau `_comments` field): thêm 6 field paste (Bước 1)
- [ ] `TypeRenderForm.cs:665` (đầu `SendCurrentPendingChunk`): thêm chặn `_isPasting` → push vào queue (Bước 6)
- [ ] `TypeRenderForm.cs:185-209` (`TypeRenderForm_Shown` hoặc constructor): thêm `txtRawMarkdown.ContextMenu = new ContextMenu();` (Bước 9)
- [ ] `TypeRenderForm.cs:1343` (đầu `OnFormClosing`): wait `_pasteSendTask` tối đa 3 s (Bước 8)
- [ ] `TypeRenderForm.cs` (cuối class): thêm `ProcessCmdKey`, `IsPasteKeyCombo`, `TryHandlePaste`, `NormalizePastedText`, `PerformFastPaste`, `EnqueuePasteOperations`, `SafeChunkLength`, `EnsurePasteSenderRunning`, `PasteSenderLoop` (Bước 2-5)

Không sửa: `TypeRenderForm.Designer.cs`, `SocketClient.cs`, `ClientHandler.cs`.

---

## 6. Rủi ro & cách giảm thiểu

| Rủi ro | Khả năng | Giảm thiểu |
|--------|----------|------------|
| Server reject op > 5 ký tự nếu chunk có emoji + ký tự thường lẫn lộn | Thấp | `MaxCharsPerPacket = 5` không đổi; `SafeChunkLength` đảm bảo không cắt surrogate ⇒ vẫn ≤ 5 char-codeunit (= server count) |
| Race condition `_clientRevision` giữa typing và paste sender | Trung bình | (i) `Interlocked.Increment` ở Bước 5; (ii) chuyển typing path vào queue khi `_isPasting=true` ở Bước 6 |
| Sender task hang khi server treo | Cao (nếu mất mạng) | `Request` đã có `timeoutMs: 5000`; thêm short-circuit nếu `!IsLoggedIn` (Bước 4 test case 7) |
| Paste qua chuột phải (menu context) không qua intercept | Trung bình | `txtRawMarkdown.ContextMenu = new ContextMenu()` để vô hiệu menu mặc định (Bước 9) |
| Memory growth do queue lớn | Rất thấp | Paste 1 MB text → 200K chunk × ~50 byte/entry ≈ 10 MB. Vẫn OK với .NET process. |

---

## 7. Optional follow-up (KHÔNG bắt buộc trong PR này)

1. **Async-hoá luôn typing path:** Đổi `SendCurrentPendingChunk` sang `Task.Run(...)` hoặc dùng kênh chung với sender task. Trade-off: ack-rev đến trễ hơn so với UI cảm nhận → caret nhảy nếu user gõ rất nhanh khi mất kết nối tạm thời.
2. **Nâng giới hạn server từ 5 → 256:** Sửa `ClientHandler.cs:1063`, đổi `MaxCharsPerPacket` client lên cùng giá trị. Giảm số round-trip 50 lần khi paste. Phải test lại OT engine với op text dài. — Đề xuất làm trong PR riêng kèm migration test.
3. **Dùng `RichTextBox` thay `TextBox`** cho editor: hỗ trợ `Paste(DataFormats.Text)` API gọn hơn, undo/redo tốt hơn, dễ subclass `WndProc`. Tuy nhiên đây là refactor lớn (font, syntax-highlight tương thích, perf khi nhiều dòng). Để dành sau.

---

## 8. Tham chiếu code nhanh

| Khái niệm | File:dòng |
|-----------|-----------|
| `txtRawMarkdown_TextChanged` (entry typing tracking) | `TypeRenderForm.cs:453` |
| `TrackRealtimeEditOps` | `TypeRenderForm.cs:508` |
| `ComputeTextDelta` | `TypeRenderForm.cs:547` |
| `SendCurrentPendingChunk` (nơi gọi socket đồng bộ) | `TypeRenderForm.cs:663` |
| `HandleOpBroadcast` | `TypeRenderForm.cs:691` |
| `FlushPendingEditOperation` | `TypeRenderForm.cs:646` |
| `OnFormClosing` | `TypeRenderForm.cs:1343` |
| `SocketClient.SendInsertOps` (đồng bộ — gốc lag) | `Network/SocketClient.cs:352` |
| Server chặn > 5 ký tự | `Server/Network/ClientHandler.cs:1062` |
| `MaxCharsPerPacket = 5` (client) | `TypeRenderForm.cs:44` |

Đây là toàn bộ thông tin cần cho AI/Dev khác implement PR. Đọc theo thứ tự §0 → §1 → §3 → §4. §5 dùng như checklist tự kiểm tra trước khi commit.
