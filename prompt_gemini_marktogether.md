Bạn là một senior C# developer. Dưới đây là toàn bộ code của ứng dụng **MarkTogether** — collaborative Markdown editor dùng TCP Socket, C# WinForms (.NET Framework 4.7.2).

Hãy đọc kỹ toàn bộ code trước khi làm bất cứ điều gì.

---

## Tổng quan kiến trúc hiện tại

- `SocketClient` là singleton, dùng mô hình **synchronous request-reply**: gửi packet → nhận response ngay trong `lock (_requestSync)`.
- `ClientHandler` ở server chạy trên thread riêng, vòng lặp đọc packet → dispatch → trả response.
- Giao thức dùng `Packet` (JSON), gửi qua TCP với 4 byte length-prefix.
- Tất cả payload models đã định nghĩa đầy đủ trong `Packet.cs`.

---

## Vấn đề kiến trúc cần giải quyết TRƯỚC

`SocketClient` hiện tại **không thể nhận `OP_BROADCAST`** vì mọi `Receive()` đều nằm trong request-reply lock — không có thread nào lắng nghe server push.

**Yêu cầu:** Refactor `SocketClient` để hỗ trợ **background listener thread** nhận các packet server push (`OP_BROADCAST`) trong khi vẫn giữ nguyên request-reply cho các packet thông thường. Cụ thể:

- Thêm một background thread (hoặc `Task`) trong `SocketClient` bắt đầu sau khi login thành công, liên tục đọc packet từ stream.
- Nếu packet nhận được là `OP_BROADCAST` → raise event `BroadcastReceived`.
- Nếu là packet response thông thường (`OK`, `ERROR`, `DOC_*`, `AUTH_RESPONSE`) → đưa vào một `Queue<Packet>` hoặc `BlockingCollection<Packet>` để `SendAndReceive` lấy ra.
- Đảm bảo thread-safe giữa background thread và các lời gọi `SendAndReceive`.
- Thêm event: `public event Action<Payload_OP_BROADCAST> BroadcastReceived;`

---

## 4 tính năng cần implement

### 1. Nhận và áp dụng `OP_BROADCAST` trong `TypeRenderForm`

Server sẽ broadcast thao tác của người dùng khác về client. Client cần áp dụng vào editor.

**Yêu cầu:**
- Trong constructor của `TypeRenderForm`, đăng ký `SocketClient.Instance.BroadcastReceived += OnBroadcastReceived;`
- Hủy đăng ký trong `OnFormClosed`.
- Implement `OnBroadcastReceived(Payload_OP_BROADCAST broadcast)`:
  - Bỏ qua nếu `broadcast.docID != _docId`.
  - Dùng `Invoke` để chạy trên UI thread.
  - **Tắt event `TextChanged`** trước khi áp dụng: `txtRawMarkdown.TextChanged -= txtRawMarkdown_TextChanged;`
  - Lưu lại `SelectionStart` hiện tại.
  - Với mỗi `op` trong `broadcast.ops`:
    - Nếu `broadcast.opType == "insert"`: chèn `op.text` vào vị trí `op.pos`. Nếu `op.pos <= savedCursor` thì `savedCursor += op.text.Length`.
    - Nếu `broadcast.opType == "delete"`: xóa `op.text.Length` ký tự tại `op.pos`. Nếu `op.pos < savedCursor` thì `savedCursor -= Math.Min(op.text.Length, savedCursor - op.pos)`.
  - Khôi phục `SelectionStart`.
  - **Bật lại event `TextChanged`**.
  - Cập nhật `_lastMarkdownText = txtRawMarkdown.Text`.
  - Tăng `_clientRevision`.

### 2. Gửi `DOC_LEAVE` khi đóng `TypeRenderForm`

**Yêu cầu:**
- Trong `OnFormClosing`, sau khi gọi `SaveDocument`, thêm:
  ```csharp
  // [ADDED] Gửi DOC_LEAVE
  SocketClient.Instance.LeaveDocument(_docId);
  ```
- Thêm method `LeaveDocument(string docId)` vào `SocketClient`:
  - Tạo packet `DOC_LEAVE` với payload `Payload_DOC_LEAVE_Request { docID = docId }`.
  - Gán `packet.Token = Token`.
  - Gọi `SendAndReceive` (không cần xử lý response, chỉ fire-and-forget hoặc ignore lỗi).
- Thêm handler `HandleDocLeave` vào `ClientHandler`:
  - Ghi log client rời tài liệu.
  - Trả về `MessageType.OK`.

### 3. Chia sẻ tài liệu — `DOC_SHARE` trong `HomeForm`

**Yêu cầu:**
- Thêm nút `btnShare` (hoặc context menu item "Chia sẻ") vào `HomeForm` bên cạnh danh sách tài liệu. Nếu cần tạo `HomeForm.Designer.cs` thay đổi thì liệt kê rõ.
- Khi bấm (với item đang chọn):
  ```csharp
  string targetUsername = Microsoft.VisualBasic.Interaction.InputBox(
      "Nhập username người muốn chia sẻ:", "Chia sẻ tài liệu", "");
  ```
  - Nếu rỗng thì return.
  - Gọi `SocketClient.Instance.ShareDocument(selectedDoc.docID, targetUsername)`.
  - Hiện `MessageBox` thành công hoặc lỗi.
- Thêm method `ShareDocument(string docId, string targetUsername)` vào `SocketClient`:
  - Gửi `DOC_SHARE` với `Payload_DOC_SHARE_Request`.
  - Nhận response, nếu `ERROR` throw exception, nếu `OK` return.
- Thêm handler `HandleDocShare` vào `ClientHandler` và `server`:
  - Kiểm tra tài liệu tồn tại và user hiện tại là owner.
  - Tìm `targetUsername` trong `UserRepository`.
  - Nếu không tìm thấy → `SendError("Người dùng không tồn tại.")`.
  - Dùng `DocumentShareRepository` để lưu share (xem schema hiện có).
  - Trả về `MessageType.OK`.

### 4. Tham gia bằng mã chia sẻ — `DOC_JOIN_CODE` trong `HomeForm`

**Yêu cầu:**
- Thêm nút `btnJoinCode` vào `HomeForm`.
- Khi bấm:
  ```csharp
  string shareCode = Microsoft.VisualBasic.Interaction.InputBox(
      "Nhập mã chia sẻ:", "Tham gia tài liệu", "");
  ```
  - Nếu rỗng thì return.
  - Gọi `SocketClient.Instance.JoinByCode(shareCode)`.
  - Nếu thành công → gọi `OpenDocumentEditor(response.docID, response.title, response.content)`.
  - Nếu lỗi → hiện `MessageBox`.
- Thêm method `JoinByCode(string shareCode)` vào `SocketClient`.
- Thêm handler `HandleDocJoinCode` vào `ClientHandler`:
  - Tìm tài liệu theo `shareCode` trong database.
  - Nếu không tìm thấy → `SendError("Mã chia sẻ không hợp lệ.")`.
  - Trả về `Payload_DOC_JOIN_CODE_Response` với thông tin tài liệu.
- Sau khi join thành công, reload danh sách: `await LoadDocumentsAsync()`.

---

## Yêu cầu về server — `HandleOpInsert` / `HandleOpDelete`

Hiện tại `HandleOpInsert` và `HandleOpDelete` chỉ log và trả `OK`, chưa broadcast cho các client khác cùng mở tài liệu.

**Yêu cầu:**
- Sau khi nhận `OP_INSERT` hoặc `OP_DELETE` hợp lệ, server cần broadcast `OP_BROADCAST` đến tất cả client khác đang mở cùng `docID`.
- Để làm được điều này, `SocketServer` cần quản lý danh sách `ClientHandler` đang active.
- Thêm vào `SocketServer`:
  ```csharp
  private static readonly List<ClientHandler> _activeHandlers = new List<ClientHandler>();
  public static void BroadcastToOthers(string docId, int senderUserId, Packet broadcastPacket) { ... }
  ```
- Gọi `SocketServer.BroadcastToOthers(...)` từ bên trong `HandleOpInsert` và `HandleOpDelete`.
- `ClientHandler` cần expose method `SendPacket(Packet p)` để `BroadcastToOthers` gọi được.
- `ClientHandler` cũng cần track `_currentDocId` (set khi `DOC_OPEN`, clear khi `DOC_LEAVE`).

---

## Yêu cầu bắt buộc

1. **Sau khi viết xong toàn bộ code**, chạy lệnh sau để kiểm tra biên dịch:
   ```
   cd C:\Users\Admin\source\repos\NT106_Project_MarkTogether
   msbuild NT106_Project_MarkTogether.sln /p:Configuration=Debug /t:Rebuild
   ```
   Hoặc nếu dùng dotnet CLI:
   ```
   dotnet build NT106_Project_MarkTogether.sln
   ```

2. **Nếu có lỗi biên dịch**, đọc output lỗi, sửa code, build lại. Lặp cho đến khi **build thành công 0 errors**.

3. Đánh dấu mọi đoạn code thêm mới bằng comment `// [ADDED]`.

4. Không thay đổi `Packet.cs`, `PacketHelper.cs`, hay bất kỳ payload model nào đã có.

5. Không thay đổi logic của các tính năng đã hoạt động (Login, Register, DocList, DocCreate, DocOpen, DocSave, OP_INSERT/DELETE gửi đi).

6. Nếu cần tạo file mới (ví dụ `ShareDocumentForm.cs`), tạo đầy đủ cả phần logic lẫn Designer nếu là WinForms, hoặc dùng `InputBox` để tránh tạo form mới.

7. Với `Microsoft.VisualBasic.Interaction.InputBox`: thêm reference `Microsoft.VisualBasic` nếu chưa có trong `Client.csproj`.

8. Xuất ra **toàn bộ nội dung của từng file đã chỉnh sửa** (không chỉ diff), để tôi có thể copy-paste thay thế trực tiếp.