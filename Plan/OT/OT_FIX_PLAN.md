# Kế hoạch sửa lỗi OT / Realtime Sync — MarkTogether

> Tài liệu này ghi lại đầy đủ phân tích + kế hoạch sửa đã được duyệt.
> AI implement chỉ cần đọc file này rồi thực thi theo đúng thứ tự.

---

## Tóm tắt phạm vi

Sửa **6 mục** liên quan đến OT / realtime sync. Không refactor lớn, không đổi kiến trúc, không sửa module ngoài phạm vi.

---

## File liên quan

| File | Vai trò |
|------|---------|
| `MarkTogether.Client/TypeRenderForm.cs` | Client OT: tracking op, gửi op, xử lý broadcast |
| `MarkTogether.Server/OT/OTEngine.cs` | 4 quy tắc transform |
| `MarkTogether.Server/OT/DocumentState.cs` | Lock per-doc, apply op |
| `MarkTogether.Server/Network/ClientHandler.cs` | `ProcessRealtimeOp`, `HandleDocOpen` |

---

## Fix #1 — Client KHÔNG cập nhật `_clientRevision` khi nhận `OP_BROADCAST` (ROOT CAUSE LỆch)

### Vấn đề
`HandleOpBroadcast` áp op của user khác vào text nhưng **không tăng `_clientRevision`**.

Khi client gõ tiếp và gửi op với `clientRevision = N` (cũ), server gọi `GetOpsSince(N)` → trả về lại op của user khác mà client đã áp → **transform thêm lần 2 → shift pos sai → text lệch**.

### Nơi sửa
File: `MarkTogether.Client/TypeRenderForm.cs`
Hàm: `HandleOpBroadcast` (khoảng dòng 743)

### Thay đổi cụ thể
Sau khi áp xong tất cả ops trong `BeginInvoke`, **TRƯỚC khi** `_suppressOpTracking = false`, thêm dòng:

```csharp
// Đồng bộ clientRevision theo serverRevision của broadcast để tránh double-transform.
int broadcastRev = p.clientResivion; // server đã gán = state.ServerRevision
if (broadcastRev > 0)
    System.Threading.Interlocked.Exchange(ref _clientRevision,
        Math.Max(broadcastRev, System.Threading.Interlocked.CompareExchange(ref _clientRevision, 0, 0)));
```

Hoặc đơn giản hơn vì `_clientRevision` chỉ được đọc/ghi trên background thread sender và broadcast handler (không phải UI thread):

```csharp
if (p.clientResivion > _clientRevision)
    System.Threading.Interlocked.Exchange(ref _clientRevision, p.clientResivion);
```

**Vị trí chính xác để chèn** — ngay sau dòng:
```csharp
_lastMarkdownText = current;
Logger.Log($"[OT] Broadcast applied: ...");
```
và trước `finally { _suppressOpTracking = false; }`.

### Lưu ý quan trọng
- `_clientRevision` là `int` bình thường (không phải `volatile`). Dùng `Interlocked.Exchange` để thread-safe.
- Chỉ tăng monotonically (không được giảm).
- `p.clientResivion` trong `Payload_OP_BROADCAST` là `serverRevision` lúc server apply op đó (xem `ClientHandler.cs:1174` — `clientResivion = state.ServerRevision`).

---

## Fix #2 — `OTEngine.TransformDeleteAgainstInsert` chưa xử lý insert GIỮA delete range

### Vấn đề
File: `MarkTogether.Server/OT/OTEngine.cs`, hàm `TransformDeleteAgainstInsert` (dòng 51–67).

Rule hiện tại:
```csharp
if (op2.pos <= op1.pos)
    result.pos += op2.text.Length;
```

**Thiếu case**: khi insert nằm GIỮA range delete (`op1.pos < op2.pos < op1.pos + deleteLength`):
- Op1 (delete) định xóa từ `op1.pos` đến `op1.pos + deleteLength`.
- Op2 (insert) vừa chèn text vào `op2.pos` — bên trong range đó.
- Nếu transform để nguyên thì delete sẽ xóa luôn insert text của user khác.
- Đúng đắn là: **giảm delete length** bằng số chars insert, hoặc skip phần overlap.

### Thay đổi cụ thể
Thay thế toàn bộ hàm `TransformDeleteAgainstInsert`:

```csharp
public static EditOpItem TransformDeleteAgainstInsert(EditOpItem op1, int deleteLength, EditOpItem op2)
{
    var result = new EditOpItem
    {
        pos = op1.pos,
        text = op1.text,
        timestamp = op1.timestamp
    };

    int insertLen = op2.text?.Length ?? 0;
    if (insertLen == 0) return result;

    if (op2.pos <= op1.pos)
    {
        // Insert hoàn toàn trước delete → shift pos về sau
        result.pos += insertLen;
    }
    else if (op2.pos < op1.pos + deleteLength)
    {
        // Insert nằm GIỮA range delete → text bị split bởi insert.
        // Giữ nguyên pos; nếu op1.text chứa nội dung cần xóa,
        // cắt bỏ phần từ op2.pos - op1.pos trở đi (không xóa insert text).
        if (!string.IsNullOrEmpty(result.text))
        {
            int cutAt = op2.pos - op1.pos; // chars trước insert point
            if (cutAt < result.text.Length)
                result.text = result.text.Substring(0, cutAt);
        }
    }
    // Nếu op2.pos >= op1.pos + deleteLength → insert hoàn toàn sau delete → không ảnh hưởng

    return result;
}
```

**Lưu ý**: `deleteLength` tham số truyền vào là `op1.text?.Length ?? 0` (xem `DocumentState.cs:84`). `result.text` là text cần xóa. Khi rút ngắn `result.text`, độ dài delete cũng giảm tương ứng khi server dùng `transformedOp.text.Length` làm `Length`.

---

## Fix #3 — `ProcessRealtimeOp` dùng cùng `clientRevision` cho nhiều op trong cùng packet

### Vấn đề
File: `MarkTogether.Server/Network/ClientHandler.cs`, hàm `ProcessRealtimeOp`, vòng lặp `foreach (var op in ops ...)`.

Khi packet có nhiều ops: op thứ 2 dùng `clientRevision` cũ → `GetOpsSince(clientRevision)` trả về cả op thứ 1 vừa apply → transform sai.

### Thay đổi cụ thể
Trong vòng `foreach`, sau mỗi `TransformAndApply`, cập nhật `clientRevision` cục bộ:

```csharp
int currentClientRev = clientRevision; // biến cục bộ dùng thay thế
var transformedOps = new List<EditOpItem>();
foreach (var op in ops ?? new List<EditOpItem>())
{
    Console.WriteLine($"[OT] {opType.ToUpper()} user={currentUserId} " +
        $"clientRev={currentClientRev} serverRev={state.ServerRevision} " +
        $"pos={op.pos} text='{op.text?.Replace("\n", "\\n").Replace("\r", "\\r")}'");

    var transformedOp = state.TransformAndApply(op, opType, currentClientRev, currentUserId);
    transformedOps.Add(transformedOp);

    // Sau mỗi op, dùng serverRevision mới cho op kế tiếp trong cùng packet
    currentClientRev = state.ServerRevision;

    Console.WriteLine($"[OT] {opType.ToUpper()} transformed pos={transformedOp.pos} " +
        $"newServerRev={state.ServerRevision}");
}
```

Chỉ thêm biến `currentClientRev` và dòng `currentClientRev = state.ServerRevision` trong loop.

---

## Fix #4 — Prewarm `DocumentState` trong `HandleDocOpen`

### Vấn đề
`DocumentStateManager.GetOrCreate(docId)` chỉ được gọi lần đầu khi nhận op → lần gõ ký tự đầu tiên bị chậm vì phải:
1. Tạo `DocumentState` mới.
2. Gọi `GetCurrentRevision` (1 DB round-trip).

Ngoài ra `HandleDocOpen` trong `ClientHandler.cs` đã gọi `GetCurrentRevision` một lần rồi trong reply — tức gọi 2 lần tổng cộng.

### Thay đổi cụ thể
File: `MarkTogether.Server/Network/ClientHandler.cs`, hàm `HandleDocOpen`.

Thêm dòng sau ngay sau `SessionManager.JoinRoom(docId, this);` và trước `Reply(...)`:

```csharp
// Prewarm DocumentState để lần gõ đầu không mất thêm 1 DB round-trip
DocumentStateManager.GetOrCreate(docId);
```

---

## Fix #5 — Tăng `MaxCharsPerPacket` từ 5 lên 32

### Vấn đề
5 ký tự/packet × RTT 50-200 ms = gõ 100 ký tự mất 1-4 giây. Paste dài hàng nghìn ký tự cực kỳ chậm.

### Thay đổi cụ thể

**Client — `TypeRenderForm.cs` dòng 57:**
```csharp
// Cũ:
private const int MaxCharsPerPacket = 5;
// Mới:
private const int MaxCharsPerPacket = 32;
```

**Server — `ClientHandler.cs` vùng check charCount (khoảng dòng 1140-1145):**
```csharp
// Cũ:
if (charCount > 5)
{
    ReplyError(packet, $"OP_{opType.ToUpper()} chỉ được chứa tối đa 5 ký tự.");
    return;
}
// Mới:
if (charCount > 32)
{
    ReplyError(packet, $"OP_{opType.ToUpper()} chỉ được chứa tối đa 32 ký tự.");
    return;
}
```

**Phải build và deploy lại cả client + server cùng lúc.** Nếu client cũ (MaxCharsPerPacket=5) kết nối server mới (limit=32) thì vẫn OK vì client cũ vẫn gửi ≤5. Nếu client mới kết nối server cũ → server sẽ reject (lỗi "tối đa 5 ký tự") → cần deploy server trước rồi mới client.

---

## Fix #6 — Flush pending ops trước khi áp `OP_BROADCAST`

### Vấn đề
Khi `HandleOpBroadcast` được gọi trên UI thread, có thể vẫn còn `_pendingOpText` chưa gửi (chờ flush timer 250ms).

Broadcast thay đổi `txtRawMarkdown.Text` → `_lastMarkdownText` được set = text mới. Khi flush timer tick sau đó, `TrackRealtimeEditOps` so sánh `_lastMarkdownText` (sau broadcast) với `txtRawMarkdown.Text` (hiện tại) → delta tính sai nếu user đã gõ thêm sau broadcast.

Thực ra, vì `_suppressOpTracking = true` khi apply broadcast, `TrackRealtimeEditOps` không chạy trong lúc apply. Và `_lastMarkdownText` được set đúng sau broadcast. Nên vấn đề chỉ xảy ra nếu `_pendingOpText` đang có nội dung chưa gửi và pos của nó bây giờ sai do broadcast đã chèn ký tự.

### Thay đổi cụ thể
File: `MarkTogether.Client/TypeRenderForm.cs`, đầu `BeginInvoke` trong `HandleOpBroadcast`.

Ngay trước `_suppressOpTracking = true;`, thêm:

```csharp
// Flush ops chưa gửi trước khi áp broadcast để tránh pos lệch
FlushPendingEditOperation();
```

**Lưu ý**: `FlushPendingEditOperation` enqueue vào `_pasteChunkQueue` — nó KHÔNG block. UI thread return ngay sau khi enqueue.

---

## Thứ tự implement được khuyến nghị

1. Fix #5 server trước (tăng limit lên 32) — server-only deploy, không ảnh hưởng client cũ.
2. Fix #3 và Fix #4 (server-only, an toàn).
3. Fix #2 `OTEngine.TransformDeleteAgainstInsert` (server-only).
4. Fix #1 và Fix #6 (client-only).
5. Fix #5 client (tăng `MaxCharsPerPacket = 32`) — sau khi server đã deploy.

---

## Sau khi implement: Test bắt buộc

| Case | Cách test | Kết quả mong đợi |
|------|-----------|------------------|
| Case 1 — 1 user gõ bình thường | Gõ 50+ ký tự liên tục | Text đúng, không lag bất thường |
| Case 2 — 2 user gõ ở vị trí khác nhau | A gõ đầu doc, B gõ cuối doc cùng lúc | Text trên cả 2 client hội tụ về cùng kết quả |
| Case 3 — 2 user gõ cùng vùng | A và B cùng gõ quanh pos=10 | Không mất ký tự, không nhân đôi, hội tụ |
| Case 4 — gõ nhanh, paste dài | Paste 500 ký tự | Text đúng, server log `[OT] INSERT` liên tục, nhanh hơn trước ~6× |
| Case 5 — gõ đồng thời sau một bên gõ trước | A gõ 10 ký tự; khi broadcast về B, B tiếp tục gõ | Text B không bị shift thêm (fix #1) |
| Case 6 — load doc lớn | Mở doc ~50KB | Load nhanh hơn, ký tự đầu gửi không lag |

---

## Cập nhật `DOCUMENTATION.md` sau khi implement

Cần cập nhật mục **10.3. Đồng bộ revision & conflict resolution** để thêm:

> Khi client nhận `OP_BROADCAST`, client cập nhật `_clientRevision = max(_clientRevision, broadcast.clientResivion)` để tránh double-transform. Server broadcast `clientResivion = state.ServerRevision` sau khi apply.

Và mục **7.9** cập nhật:
- `MaxCharsPerPacket` = 32 (không còn là 5).
- Flush pending trước khi áp broadcast.

---

## Fix #7 — Client `HandleOpBroadcast` dùng `txtRawMarkdown.Text = ...` reset scroll position

### Vấn đề
File: `MarkTogether.Client/TypeRenderForm.cs`, hàm `HandleOpBroadcast`.

Khi nhận broadcast, code cũ xây dựng string `current` trong bộ nhớ rồi gán một lần:
```csharp
txtRawMarkdown.Text = current;          // WM_SETTEXT → scroll về đầu trang
txtRawMarkdown.SelectionStart = caret;  // khôi phục con trỏ logic
// ← scroll position vẫn ở top!
```

`WM_SETTEXT` reset **cả `SelectionStart` về 0 lẫn scroll position về đầu trang**. Dòng tiếp theo chỉ khôi phục `SelectionStart`, không khôi phục scroll → user thấy view nhảy lên đầu trang mỗi khi nhận broadcast.

### Thay đổi cụ thể
File: `MarkTogether.Client/TypeRenderForm.cs`, bên trong `BeginInvoke` của `HandleOpBroadcast`.

Thay toàn bộ đoạn build `current` + `txtRawMarkdown.Text = current` bằng **surgical `SelectedText =` edits**:

```csharp
foreach (var op in p.ops ?? Enumerable.Empty<EditOpItem>())
{
    if (op == null) continue;
    string current = txtRawMarkdown.Text ?? string.Empty;
    int pos = Math.Max(0, Math.Min(op.pos, current.Length));
    string text = op.text ?? "";
    if (p.opType == "insert")
    {
        txtRawMarkdown.SelectionStart = pos;
        txtRawMarkdown.SelectionLength = 0;
        txtRawMarkdown.SelectedText = text;
        if (caret >= pos) caret += text.Length;
    }
    else // delete
    {
        int len = Math.Min(text.Length, current.Length - pos);
        if (len > 0)
        {
            txtRawMarkdown.SelectionStart = pos;
            txtRawMarkdown.SelectionLength = len;
            txtRawMarkdown.SelectedText = "";
            if (caret > pos) caret -= Math.Min(len, caret - pos);
        }
    }
}

int finalLen = txtRawMarkdown.TextLength;
txtRawMarkdown.SelectionStart = Math.Max(0, Math.Min(caret, finalLen));
txtRawMarkdown.SelectionLength = 0;
_lastMarkdownText = txtRawMarkdown.Text ?? string.Empty;
```

**Tại sao không cần `ScrollToCaret()`**: `SelectedText =` chỉ thay đổi vùng được chọn, không gửi `WM_SETTEXT` → scroll position giữ nguyên hoàn toàn. Không có flash scroll-to-top, không cần khôi phục lại.

**Lưu ý**: `_suppressOpTracking = true` vẫn được set trước vòng lặp, nên mỗi `TextChanged` do `SelectedText =` kích hoạt đều chỉ chạy nhánh `_lastMarkdownText = txtRawMarkdown.Text` (không track op). Render debounce timer restart sau mỗi op nhưng do debounce nên chỉ render 1 lần sau khi hết loop.

---

## Thứ tự implement được khuyến nghị

1. Fix #5 server trước (tăng limit lên 32) — server-only deploy, không ảnh hưởng client cũ.
2. Fix #3 và Fix #4 (server-only, an toàn).
3. Fix #2 `OTEngine.TransformDeleteAgainstInsert` (server-only).
4. Fix #1 và Fix #6 (client-only).
5. Fix #5 client (tăng `MaxCharsPerPacket = 32`) — sau khi server đã deploy.
6. Fix #7 (client-only, độc lập).

---

## Ghi chú rủi ro còn lại

- **Reconnect**: vẫn chưa có cơ chế tái đồng bộ sau reconnect. Khi kết nối lại, user cần đóng và mở lại doc để nhận state mới nhất.
- **Multi-backend collision**: nếu LB remap doc giữa 2 backend, `ServerRevision` in-memory có thể conflict. Đây là edge case hiếm (cần `DOC_REMAP_TIMEOUT = 120s` hết hạn) — chưa sửa trong đợt này.
- **`TransformDeleteAgainstInsert` đã sửa nhưng `TransformInsertAgainstDelete` giữ nguyên** — case delete trước insert: nếu delete xóa vùng chứa insert point, insert pos bị set = `op2.pos` (chính xác) → không bị mất insert. Logic này đã đúng.

---

## Đợt 2 — Xem `OT_FIX_PLAN_V2.md` (2026-05-26)

Tách pipeline edit thành 4 case (Typing / BulkInsert / BulkDelete / ReplaceBlock). Thêm typing debounce 120ms cho IME tiếng Việt. Tăng giới hạn server từ 32 lên 8192 ký tự / packet và batch broadcast `transformedOps` trong một `OP_BROADCAST`. Báo cáo: `OT_FIX_IMPLEMENTATION_REPORT_V2.md`.
