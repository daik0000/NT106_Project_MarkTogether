# Changelog — 22/05/2026

Phạm vi: sửa 3 vấn đề được báo cáo sau khi test với người dùng thực.

---

## 1. Chẩn đoán freeze khi gõ ký tự đặc biệt (`TypeRenderForm.cs`, `Client.csproj`)

### Nguyên nhân tiềm ẩn

Code hiện tại đã xử lý đúng threading:
- Render Markdig → `Task.Run` (background thread).
- Gửi OT op → background `PasteSenderLoop`, không block UI.
- Autosave → `Task.Run` mỗi 30 s.

Không có call DB/API nào trực tiếp trên UI thread khi gõ. Freeze có thể do:
- Máy người dùng chậm → Markdig + WebView2 `ExecuteScriptAsync` mất nhiều ms hơn dự kiến.
- `Logger.cs` tồn tại trong project nhưng **chưa được include vào `Client.csproj`** → mọi log diagnostic từ trước đều bị bỏ qua.

### Thay đổi

**`MarkTogether.Client/Client.csproj`**
- Thêm `<Compile Include="Network\Logger.cs" />` để Logger được build vào client.

**`MarkTogether.Client/TypeRenderForm.cs`**
- `txtRawMarkdown_TextChanged`: bọc toàn bộ body trong `try/catch`. Nếu delta/queue logic ném exception, WinForms message pump không bị crash; exception được log với `textLen` (không log nội dung).
- `RenderMarkdown`: thêm `Stopwatch` đo thời gian. Log dòng cảnh báo nếu render > 500 ms (slow machine). Đổi `catch { return; }` thành `catch (Exception ex)` để log tên exception + message thay vì bỏ qua hoàn toàn.
- `UpdatePreviewBodyAsync`: đổi `catch { }` thành `catch (Exception ex)` để log khi `ExecuteScriptAsync` thất bại (WebView2 busy / mất context).

### Cách đọc log

File log: `<exe-directory>\client_debug_<PID>.log`

Dòng đáng chú ý:
```
[Render] slow markdown render: 1234ms textLen=5678
[Render] markdown render FAILED after 56ms textLen=300: NullReferenceException: ...
[Preview] ExecuteScriptAsync failed (htmlLen=...): ...
[Editor] TextChanged exception (textLen=...): ...
```

---

## 2. Fix import `.md` mất xuống dòng (`HomeForm.cs`)

### Nguyên nhân gốc

`txtRawMarkdown` là `System.Windows.Forms.TextBox` (multiline). TextBox WinForms **không hiển thị xuống dòng** với ký tự `\n` đơn lẻ (Unix/Linux line ending) — phải là `\r\n` (CRLF, Windows). File `.md` được tạo trên Linux/macOS hoặc bằng editor Unix-style sẽ dùng LF, khiến toàn bộ nội dung hiện thành một dòng duy nhất trong editor.

### Thay đổi

**`MarkTogether.Client/HomeForm.cs`**

Thêm `using System.Text;`.

Trong `btnImportMd_Click`, đoạn đọc file:

```csharp
// Trước:
string content = await Task.Run(() => File.ReadAllText(filePath));

// Sau:
string content = await Task.Run(() =>
{
    string raw = File.ReadAllText(filePath, Encoding.UTF8);
    // Normalize mọi line ending (LF / CR / CRLF) → CRLF để TextBox WinForms hiển thị đúng
    return raw.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
});
```

Thuật toán normalize: `CRLF → LF` → `CR → LF` → `LF → CRLF` — đảm bảo idempotent.

### Kết quả

File `.md` dù có LF, CRLF hay CR đều hiển thị đúng xuống dòng trong raw editor sau khi import. Preview Markdig không bị ảnh hưởng (Markdig xử lý cả LF lẫn CRLF).

---

## 3. Thêm UI xóa tài liệu (`SocketClient.cs`, `HomeForm.cs`)

### Bối cảnh

Server đã có `HandleDocDelete` (ClientHandler.cs:398) và `Payload_DOC_DELETE_Request / Response` (Packet.cs). Chỉ thiếu:
- Client API method.
- UI để người dùng gọi.

### Thay đổi

**`MarkTogether.Client/Network/SocketClient.cs`**

Thêm method:
```csharp
public Payload_DOC_DELETE_Response DeleteDocument(string docId)
{
    EnsureAuthenticated();
    var response = Request(MessageType.DOC_DELETE,
        new Payload_DOC_DELETE_Request { docID = docId });
    return ExtractOrThrow<Payload_DOC_DELETE_Response>(response, MessageType.DOC_DELETE);
}
```

**`MarkTogether.Client/HomeForm.cs`**

Thêm 4 method (không đụng Designer):

| Method | Mô tả |
|--------|-------|
| `BuildDocumentContextMenu()` | Tạo `ContextMenuStrip` gắn vào `listDocuments` khi Load. Menu item "Xóa tài liệu" chỉ enable khi `permission == "owner"`. |
| `GetSelectedDocument()` | Helper trả `DocInfo` đang được chọn. |
| `DeleteSelectedDocumentAsync()` | Xác nhận → `SocketClient.DeleteDocument` → reload danh sách. Default button là "Không" để chống xóa nhầm. |
| `HomeForm_KeyDown` (cập nhật) | Thêm case `Keys.Delete` — chỉ kích hoạt khi `listDocuments.Focused` để tránh xóa nhầm khi đang gõ mã join code. |

### UX Flow

```
Người dùng right-click tài liệu trong danh sách
  → Menu "Xóa tài liệu" (disabled nếu không phải owner)
  → Confirm dialog: "Xóa tài liệu 'Title'?" — default Button2 = "Không"
  → Task.Run DeleteDocument → server soft-delete + DOC_RELOAD_BROADCAST
  → LoadDocumentsAsync() — danh sách refresh
```

Hoặc: focus vào danh sách → chọn tài liệu → nhấn `Delete`.

### Ràng buộc giữ nguyên

- Server chỉ cho phép owner xóa (RBAC `IsOwner`). Client cũng kiểm tra trước để thông báo rõ ràng thay vì chờ server reject.
- Xóa là soft delete (`deleted_at`). Khôi phục vẫn phải thao tác DB trực tiếp (không thay đổi).
- `DOC_RELOAD_BROADCAST` broadcast tới mọi client trong room khi tài liệu bị xóa — các client đang mở sẽ nhận thông báo và reload.

---

## Files đã thay đổi

| File | Loại thay đổi |
|------|---------------|
| `MarkTogether.Client/TypeRenderForm.cs` | Thêm try/catch + Stopwatch logging |
| `MarkTogether.Client/HomeForm.cs` | Fix newline import + thêm delete UI |
| `MarkTogether.Client/Network/SocketClient.cs` | Thêm `DeleteDocument()` |
| `MarkTogether.Client/Client.csproj` | Thêm `Logger.cs` vào compile list |
