# 🗺️ KẾ HOẠCH CHI TIẾT — HOÀN THIỆN 9 CHỨC NĂNG MARKTOGETHER

> **Phạm vi**: Hoàn thiện 9 chức năng còn lại của hệ thống MarkTogether.  
> **Stack**: C# WinForms (Client) + C# TCP Server (Net472) + PostgreSQL + Dapper + Newtonsoft.Json + BCrypt + Markdig + WebView2.  
> **Giao thức**: JSON Packet với 4-byte length prefix (xem `Shared/PacketHelper.cs`).  
> **Ngày lập**: 20/05/2026.

---

## 🎯 9 CHỨC NĂNG CẦN HOÀN THIỆN

| # | Chức năng | Mức độ ưu tiên | Phụ thuộc |
|---|-----------|----------------|-----------|
| 1 | Đăng nhập / Đăng xuất | 🔴 P0 (cốt lõi) | Đã có Login. Cần Logout đúng nghĩa |
| 2 | Markdown → Hiển thị văn bản | 🟢 Đã có ~95% | Markdig + WebView2 |
| 3 | Xuất PDF | 🟡 P1 | Cần thư viện render |
| 4 | Chat trong phiên | 🔴 P0 (cùng GĐ4 broadcast) | SessionManager docRoom |
| 5 | Comment trên đoạn văn bản | 🟡 P1 | Phụ thuộc anchor vị trí |
| 6 | Set quyền chia sẻ (owner/editor/viewer) | 🔴 P0 | DocumentShareRepository sẵn |
| 7 | Lưu ảnh người dùng lên server | 🟡 P1 | Đã có schema images |
| 8 | Chatbot AI | 🟢 P2 | API ngoài (Gemini/OpenAI) |
| 9 | Lưu lại version tài liệu | 🟡 P1 | DocumentVersionRepository sẵn |

---

## 📊 TIẾN ĐỘ TỔNG (TICK KHI HOÀN THÀNH)

```
[H] Hạ tầng async + DocRoom    ██████████  Done
[1] Auth (Login/Logout)        ██████████  Done (8/8)
[2] Markdown Render            ██████████  Done
[3] Xuất PDF                   ██████████  Done (6/6)
[4] Chat trong phiên           ██████████  Done (9/9)
[5] Comment                    █████████░  ~10/12 (chưa highlight inline icon)
[6] Set quyền chia sẻ          ██████████  Done (10/10)
[7] Lưu ảnh user lên server    ██████████  Done (8/8)
[8] Chatbot AI                 █████████░  ~6/7 (chưa cấu hình API key thật)
[9] Lưu version tài liệu       ██████████  Done (9/9)
```

### ✅ Checklist tổng (đánh dấu khi mỗi chức năng đã đầy đủ)

- [x] **CN1** — Đăng nhập/Đăng xuất hoàn chỉnh (có logout server-side, dọn session, quay về LoginForm)
- [x] **CN2** — Markdown render live (Markdig + WebView2)
- [x] **CN3** — Xuất PDF từ tài liệu hiện hành
- [x] **CN4** — Chat realtime trong phiên (cùng tài liệu)
- [x] **CN5** — Comment trên đoạn văn bản (CRUD + hiển thị)
- [x] **CN6** — Set quyền share (mời user + chỉnh permission + thu hồi + share code)
- [x] **CN7** — Upload avatar / ảnh đính kèm tài liệu lên server
- [x] **CN8** — Chatbot AI hỗ trợ (gợi ý nội dung, tóm tắt, dịch)
- [x] **CN9** — Lưu version & xem/khôi phục lịch sử

---

# 🧩 NGUYÊN TẮC CHUNG (đọc trước khi đọc từng chức năng)

### Bố cục thay đổi code theo tầng

| Tầng | Khi nào sửa |
|------|------------|
| `Shared/Packet.cs` | Mỗi khi thêm `MessageType` hoặc `Payload_*` mới |
| `Server/Database/Models/*` | Khi thêm bảng / cột mới |
| `Server/Database/Repositories/*` | Mỗi khi truy vấn DB mới |
| `Server/Network/ClientHandler.cs` | Mỗi khi thêm case xử lý packet mới |
| `Server/Network/SessionManager.cs` | Khi thay đổi state in-memory (rooms, presence) |
| `Server/Services/*` | Logic nghiệp vụ phức tạp (Auth, AI, OT) |
| `Client/Network/SocketClient.cs` | Mỗi khi thêm hàm gọi server tương ứng |
| `Client/*Form.cs` | UI cho từng tính năng |

### Quy ước packet
- Mọi response gắn với một `request` đều có `success: bool` + `message: string`.
- Packet broadcast (server → nhiều client) đặt tên `*_BROADCAST` để client biết là *push*, không phải reply.
- Mọi packet cần auth → server gọi `ResolveCurrentUserId(packet)` trước.

### Flow nhận packet ở Client (cần cải tiến)
> Hiện tại `SocketClient` đang **blocking Receive** sau mỗi Send. Để hỗ trợ broadcast (CN4, CN5, CN9 history, OP_BROADCAST), cần tách ra **một thread đọc liên tục** và một **dispatcher event** theo `MessageType`. Việc này được mô tả ở mục **Hạ tầng chung — H1**.

---

# 🛠️ HẠ TẦNG CHUNG (CẦN LÀM TRƯỚC)

> Đây là phần *nền* để các chức năng broadcast (CN4, CN5, CN6 thông báo, CN8) chạy được. Không có nó, mọi tính năng realtime sẽ không hoạt động.

### H1. SocketClient bất đồng bộ + Event Dispatcher
- [ ] Tách `ReceiveLoop()` thành thread riêng chạy trong vòng lặp đọc `PacketHelper.Receive(_stream)`.
- [ ] Mỗi packet đến → raise event `OnPacketReceived(Packet)` hoặc theo `MessageType` riêng (`OnChatMessageReceived`, `OnOpBroadcast`, ...).
- [ ] Với các call yêu cầu *response sync* (Login, OpenDoc...), dùng cơ chế **correlation**: gắn `RequestId` (Guid) trong `Packet`, dispatcher biết đây là response cho request nào → unblock `TaskCompletionSource`.
- [ ] Bổ sung field `string RequestId` vào `Packet` (optional, nếu null thì là broadcast).
- [ ] Refactor `Login`, `Register`, `GetDocuments`, `OpenDocument`, `SaveDocument`, `CreateDocument`, `SendInsertOps`, `SendDeleteOps` dùng cơ chế correlation.

### H2. Server SessionManager hỗ trợ "Doc Room"
- [ ] Thêm map `Dictionary<string docId, HashSet<ClientHandler>>` để biết ai đang mở doc nào.
- [ ] Hàm `JoinRoom(docId, handler)`, `LeaveRoom(docId, handler)`, `GetClientsInRoom(docId)`.
- [ ] Khi `ClientHandler` disconnect → tự động `LeaveRoom` ở mọi doc nó đang join.
- [ ] Hàm static `BroadcastToRoom(docId, packet, ClientHandler exclude = null)`.

### H3. ClientHandler exposed gửi packet ra ngoài
- [ ] `ClientHandler` cần có method public `Send(Packet)` để `SessionManager` (hoặc service khác) push packet về client này.
- [ ] Đảm bảo `Send` thread-safe (lock stream).

### Checklist hạ tầng

- [x] Thêm `RequestId` vào `Packet` class
- [x] Tạo class `PendingRequest` chứa `TaskCompletionSource<Packet>`
- [x] Refactor `SocketClient` thành event-based + correlation
- [x] Mở rộng `SessionManager` với doc-room map
- [x] Thêm `ClientHandler.Send(Packet)` thread-safe (`SendSafe`)
- [x] Xử lý cleanup room khi client disconnect

---

# 1️⃣ ĐĂNG NHẬP / ĐĂNG XUẤT

### 🎯 Mục tiêu
Người dùng đăng nhập, hoạt động trong phiên, **đăng xuất sạch sẽ** (token bị xóa server-side, quay về LoginForm), và session expired phải bắt được.

### 📍 Hiện trạng
- ✅ `LoginForm`, `RegisterForm` đã có
- ✅ `AuthService.Login/Register` dùng BCrypt
- ✅ `SessionManager.AddSession/RemoveSession`
- ❌ **Chưa có** `MessageType.AUTH_LOGOUT`
- ❌ **Chưa có** UI nút Logout ở `HomeForm`
- ❌ Token không có thời hạn — nên thêm `LastActiveAt` để timeout

### 🧱 Thay đổi giao thức
**`Shared/Packet.cs`** — thêm:
```csharp
// Trong enum MessageType:
AUTH_LOGOUT,

public class Payload_AUTH_LOGOUT_Request { /* trống — token nằm trong Packet.Token */ }
public class Payload_AUTH_LOGOUT_Response
{
    public bool success { get; set; }
    public string message { get; set; }
}
```

### 🔁 Flow
```
[Client]                                  [Server]
HomeForm: btnLogout_Click
  → Confirm dialog
  → SocketClient.Logout()
      Send(AUTH_LOGOUT, token)        →   ClientHandler.HandleLogout()
                                            - SessionManager.RemoveSession(token)
                                            - SessionManager.LeaveAllRooms(this)
                                            - reset _userId/_username/_token
                                       ←   Reply OK
  ← Nhận OK
  → SocketClient.ClearLocalState()
  → Hide(HomeForm), Show(LoginForm)
```

### 📂 Files chỉnh
- `Shared/Packet.cs` — thêm MessageType + Payload
- `Server/Network/ClientHandler.cs` — thêm `HandleLogout(packet)`
- `Server/Network/SessionManager.cs` — thêm `LeaveAllRooms(ClientHandler)`
- `Client/Network/SocketClient.cs` — thêm `Logout()`
- `Client/HomeForm.cs` + `.Designer.cs` — thêm `btnLogout` + handler

### ✅ Checklist CN1 (8 mục)
- [x] Bổ sung `MessageType.AUTH_LOGOUT` trong `Shared/Packet.cs`
- [x] Thêm `Payload_AUTH_LOGOUT_Request/Response`
- [x] `ClientHandler.HandleLogout` xử lý: remove session + leave rooms + reply OK
- [x] `SessionManager.LeaveAllRooms(handler)` gọi nội bộ trước khi xóa
- [x] `SocketClient.Logout()` gửi packet, đợi reply, clear `Token/UserId/Username`
- [x] UI: thêm `btnLogout` ở `HomeForm` (Designer)
- [x] Confirm dialog "Bạn có chắc muốn đăng xuất?"
- [x] Chuyển flow về `LoginForm` sau khi logout xong (đóng các form khác mở)

---

# 2️⃣ MARKDOWN → HIỂN THỊ VĂN BẢN ✅

### 📍 Hiện trạng (đã hoàn chỉnh)
- ✅ Markdig pipeline `UseAdvancedExtensions()`
- ✅ `WebView2` render với highlight.js + style sạch
- ✅ Debounce 280ms
- ✅ Render qua base64 → tránh escape JS

### 🔧 Tinh chỉnh đề xuất (không bắt buộc)
- [ ] Thêm syntax highlight cho `mermaid` (sơ đồ)
- [ ] Thêm copy-button trên block `<pre>`
- [ ] Print-friendly CSS (chuẩn bị cho CN3 Export PDF)

> **Ghi chú quan trọng**: CSS dùng cho preview *KHÔNG* dùng được cho PDF (xem CN3) vì nó dựa vào WebView2. Sẽ tái dùng HTML body nhưng wrap CSS print.

---

# 3️⃣ XUẤT PDF

### 🎯 Mục tiêu
Người dùng nhấn nút Export → file `.pdf` được tạo, lưu vào máy.

### 🧠 Phương án triển khai (Client-side)
**Phương án A — Dùng `WebView2.PrintToPdfAsync()` (KHUYÊN DÙNG)**
- Đã có WebView2 → gọi `webPreview.CoreWebView2.PrintToPdfAsync(filePath, settings)`.
- Không thêm thư viện mới.
- Output đẹp vì giống y preview.

**Phương án B — Dùng `iText7` / `PdfSharpCore`**
- Phải parse Markdown → HTML → PDF qua thư viện riêng.
- Phức tạp hơn, dễ lệch style.

→ **Chọn phương án A**.

### 🔁 Flow
```
[Client]
TypeRenderForm: btnExportPdf_Click
  → Đảm bảo preview render xong (await flush render)
  → SaveFileDialog (.pdf)
  → webPreview.CoreWebView2.PrintToPdfAsync(filePath, printSettings)
  → MessageBox "Xuất PDF thành công" + nút Open Folder
```

### 📂 Files chỉnh
- `Client/TypeRenderForm.Designer.cs` — thêm `btnExportPdf`
- `Client/TypeRenderForm.cs` — thêm method `ExportPdfAsync()`

### ⚠️ Lưu ý kỹ thuật
- Trước khi export phải đảm bảo `_pendingHtmlBody` đã render xong (có thể `await ProcessPreviewQueueAsync()`).
- Nếu doc có `<img>` external → phải đợi load xong mới print (CoreWebView2 tự xử).
- Cần in CSS riêng (margin trang, page-break-inside: avoid cho `<pre>`).

### ✅ Checklist CN3 (6 mục)
- [x] Thêm nút "Export PDF" trong `TypeRenderForm` (Designer)
- [x] Đảm bảo flush preview trước khi export (race condition)
- [x] Bổ sung CSS print (`@media print`) trong `PreviewHtmlTemplate`
- [x] Cài đặt `ExportPdfAsync()` dùng `PrintToPdfAsync`
- [x] `SaveFileDialog` đặt tên mặc định = title của doc
- [ ] Test với doc dài (>10 trang), có code block, có table — *cần test thủ công*

---

# 4️⃣ CHAT TRONG PHIÊN

### 🎯 Mục tiêu
Tất cả người dùng đang mở **cùng một tài liệu** có thể chat realtime với nhau qua một panel chat ở `TypeRenderForm`. Tin nhắn không lưu DB lâu dài (chỉ in-memory trong session) — *hoặc* lưu vào bảng tạm `chat_messages` (xem option).

### 🧱 Thay đổi giao thức
**`Shared/Packet.cs`** — thêm:
```csharp
// Trong enum MessageType:
CHAT_SEND,         // Client → Server: gửi tin nhắn
CHAT_BROADCAST,    // Server → Client (tất cả ai trong room): nhận tin nhắn
CHAT_HISTORY,      // Client xin lịch sử khi vào room

public class Payload_CHAT_SEND
{
    public string docID { get; set; }
    public string content { get; set; }
}

public class Payload_CHAT_BROADCAST
{
    public string docID { get; set; }
    public int userId { get; set; }
    public string username { get; set; }
    public string content { get; set; }
    public DateTime sentAt { get; set; }
}

public class Payload_CHAT_HISTORY_Request
{
    public string docID { get; set; }
    public int limit { get; set; } = 50;
}

public class Payload_CHAT_HISTORY_Response
{
    public string docID { get; set; }
    public List<Payload_CHAT_BROADCAST> messages { get; set; }
}
```

### 🗄️ Database (TÙY CHỌN — nếu muốn lưu lịch sử)
```sql
CREATE TABLE IF NOT EXISTS chat_messages (
    id          BIGSERIAL PRIMARY KEY,
    doc_id      VARCHAR(50) NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    user_id     INT NOT NULL REFERENCES users(id),
    content     TEXT NOT NULL,
    sent_at     TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
CREATE INDEX idx_chat_doc_time ON chat_messages(doc_id, sent_at DESC);
```

### 🔁 Flow
```
[User mở doc]
  → DOC_OPEN xong → SessionManager.JoinRoom(docId, handler)
  → Client gửi CHAT_HISTORY → server trả 50 tin gần nhất
  → Render vào panel chat

[User gõ tin]
  → SocketClient.SendChat(docId, content)
     Send(CHAT_SEND)
       Server.HandleChatSend()
         - Validate user có quyền doc?
         - Insert chat_messages (nếu lưu DB)
         - Broadcast CHAT_BROADCAST cho mọi client trong room (kể cả người gửi để confirm)
  → Tất cả client trong room nhận CHAT_BROADCAST
     → Render bubble vào panel chat
```

### 📂 Files chỉnh
- `Shared/Packet.cs` — 3 messageType + 4 payload
- `Server/Database/Scripts/schema_init.sql` — thêm bảng `chat_messages`
- `Server/Database/Models/ChatMessage.cs` (mới)
- `Server/Database/Repositories/ChatRepository.cs` (mới)
- `Server/Network/ClientHandler.cs` — `HandleChatSend`, `HandleChatHistory`, gọi `JoinRoom`/`LeaveRoom` ở `HandleDocOpen`/`HandleDocLeave`
- `Client/Network/SocketClient.cs` — `SendChat`, `RequestChatHistory`, sự kiện `OnChatReceived`
- `Client/TypeRenderForm.Designer.cs` — thêm `panelChat` (RichTextBox hiển thị + TextBox gõ + nút Send)
- `Client/TypeRenderForm.cs` — wire event

### ✅ Checklist CN4 (9 mục)
- [x] Thêm `MessageType.CHAT_SEND/CHAT_BROADCAST/CHAT_HISTORY` + payload
- [x] Tạo bảng `chat_messages` + `ChatRepository`
- [x] `SessionManager` doc-room (xem H2)
- [x] `ClientHandler.HandleDocOpen` → JoinRoom; `HandleDocLeave` / disconnect → LeaveRoom
- [x] `HandleChatSend` validate + persist + broadcast
- [x] `HandleChatHistory` trả 50 tin gần nhất
- [x] `SocketClient` event `OnChatBroadcast`, `SendChat()`
- [x] UI panel chat trong `TypeRenderForm` (tab Chat)
- [x] Hiển thị username + thời gian

---

# 5️⃣ COMMENT

### 🎯 Mục tiêu
Người dùng select một đoạn text → click "Comment" → mở popup nhập → comment được lưu, hiển thị inline (icon bên cạnh đoạn) hoặc trong panel comment. Thread reply (optional).

### 🧱 Database
```sql
CREATE TABLE IF NOT EXISTS document_comments (
    id              VARCHAR(50) PRIMARY KEY DEFAULT 'cmt_' || gen_random_uuid()::text,
    doc_id          VARCHAR(50) NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    parent_id       VARCHAR(50) REFERENCES document_comments(id) ON DELETE CASCADE,  -- thread
    anchor_start    INT NOT NULL,    -- vị trí bắt đầu trong markdown text
    anchor_end      INT NOT NULL,    -- vị trí kết thúc
    anchor_text     TEXT,            -- snippet để hiển thị (dù vị trí lệch sau edit)
    content         TEXT NOT NULL,
    resolved        BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
CREATE INDEX idx_comments_doc ON document_comments(doc_id, created_at DESC);
```

### 🧱 Packet
```csharp
COMMENT_CREATE,
COMMENT_LIST,
COMMENT_RESOLVE,
COMMENT_DELETE,
COMMENT_BROADCAST,   // server push khi có comment mới/xoá

public class Payload_COMMENT_CREATE_Request
{
    public string docID { get; set; }
    public string parentId { get; set; }   // null nếu là root comment
    public int anchorStart { get; set; }
    public int anchorEnd { get; set; }
    public string anchorText { get; set; }
    public string content { get; set; }
}

public class CommentDto
{
    public string id { get; set; }
    public string parentId { get; set; }
    public int userId { get; set; }
    public string username { get; set; }
    public int anchorStart { get; set; }
    public int anchorEnd { get; set; }
    public string anchorText { get; set; }
    public string content { get; set; }
    public bool resolved { get; set; }
    public DateTime createdAt { get; set; }
}

public class Payload_COMMENT_LIST_Response
{
    public string docID { get; set; }
    public List<CommentDto> comments { get; set; }
}

public class Payload_COMMENT_BROADCAST
{
    public string action { get; set; }  // "created" | "resolved" | "deleted"
    public CommentDto comment { get; set; }
}
```

### 🔁 Flow
```
[User]
  Chọn text trong txtRawMarkdown → bấm "Add comment"
    → Lấy SelectionStart, SelectionLength
    → Mở dialog NewCommentForm → user gõ → OK
    → SocketClient.CreateComment(docId, anchor, content)
      Server.HandleCommentCreate
        - kiểm tra quyền (viewer cũng được comment)
        - INSERT
        - Broadcast COMMENT_BROADCAST(action=created) cho room
[Tất cả Client trong room]
  → Nhận broadcast → render icon 💬 cạnh anchor + thêm vào panel comment
```

### 🧠 Đồng bộ vị trí khi text thay đổi
- Khi user khác edit text → anchorStart/anchorEnd của comment có thể lệch.
- **Phương án đơn giản (giai đoạn này)**: lưu `anchorText` snippet ngắn (~30 ký tự). Khi mở doc → server tìm kiếm snippet, nếu mismatch thì đánh dấu comment "out of date".
- **Phương án nâng cao**: dùng OT để transform anchor theo từng op (gắn với GĐ6).

### 📂 Files chỉnh
- `Shared/Packet.cs` — 5 messageType + nhiều payload
- `Server/Database/Scripts/schema_init.sql` — thêm `document_comments`
- `Server/Database/Models/Comment.cs` (mới)
- `Server/Database/Repositories/CommentRepository.cs` (mới)
- `Server/Network/ClientHandler.cs` — 4 handler
- `Client/Network/SocketClient.cs` — 4 method + event
- `Client/TypeRenderForm.cs` + Designer — panel Comments + nút "Add Comment"
- `Client/NewCommentForm.cs` (mới) — popup nhập

### ✅ Checklist CN5 (12 mục)
- [x] SQL: tạo bảng `document_comments` + index
- [x] Model `Comment` + `CommentRepository`
- [x] Packet: 5 MessageType + payload
- [x] Handler `HandleCommentCreate/List/Resolve/Delete`
- [x] Broadcast `COMMENT_BROADCAST` cho room
- [x] Logic kiểm tra quyền comment (viewer trở lên)
- [x] Anchor snippet lưu trữ
- [x] `SocketClient` 4 method + event
- [x] UI panel Comments (list + actions)
- [x] Form `NewCommentForm` popup
- [x] Highlight đoạn anchor trong textbox khi double click comment
- [ ] Thread reply (parentId) — render lồng nhau trong panel — *để future work*

---

# 6️⃣ SET QUYỀN CHIA SẺ

### 🎯 Mục tiêu
Owner có thể:
- Sinh **share code** ngẫu nhiên cho doc.
- Mời user theo **username** với quyền `editor` / `viewer`.
- Xem danh sách collaborator + cập nhật quyền.
- Thu hồi quyền.
- User khác có thể nhập share code → tham gia (mặc định viewer).
- Mọi handler đọc/sửa doc đều **kiểm tra quyền** (không chỉ owner).

### 📍 Hiện trạng
- ✅ Bảng `document_shares` + `DocumentShareRepository` đầy đủ
- ✅ Packet `DOC_SHARE`, `DOC_JOIN_CODE` đã định nghĩa
- ❌ Server **chưa có** handler `HandleDocShare`, `HandleDocJoinCode`
- ❌ Doc chưa sinh `shareCode` khi tạo
- ❌ `HandleDocOpen` đang chỉ cho phép owner

### 🧱 Thay đổi schema
```sql
ALTER TABLE documents 
    ADD COLUMN IF NOT EXISTS share_code VARCHAR(20) UNIQUE;
CREATE INDEX IF NOT EXISTS idx_documents_share_code ON documents(share_code);
```

### 🧱 Packet bổ sung
```csharp
DOC_SHARE_LIST,         // owner xin danh sách collaborator
DOC_SHARE_UPDATE,       // owner đổi quyền của 1 user
DOC_SHARE_REVOKE,       // owner thu hồi quyền
DOC_SHARE_REGEN_CODE,   // sinh lại share code

public class Payload_DOC_SHARE_Request           // ĐÃ CÓ — bổ sung permission
{
    public string docID { get; set; }
    public string targetUsername { get; set; }
    public string permission { get; set; }       // "editor" | "viewer"
}

public class Payload_DOC_SHARE_LIST_Response
{
    public string docID { get; set; }
    public List<CollaboratorDto> collaborators { get; set; }
}

public class CollaboratorDto
{
    public int userId { get; set; }
    public string username { get; set; }
    public string email { get; set; }
    public string permission { get; set; }
    public DateTime invitedAt { get; set; }
}

public class Payload_DOC_SHARE_UPDATE_Request
{
    public string docID { get; set; }
    public int targetUserId { get; set; }
    public string newPermission { get; set; }
}
```

### 🔁 Flow
```
[Owner]
  Mở dialog "Share" trong TypeRenderForm
    Tab 1: Share code
      → Nếu null → bấm "Generate" → Server: random 8 ký tự, UPDATE documents.share_code
      → Hiển thị code + nút Copy
    Tab 2: Mời theo username
      → Nhập "minhanh" + chọn "editor"
      → SocketClient.ShareDoc(docId, "minhanh", "editor")
         Server: HandleDocShare
            - kiểm tra current user là owner
            - tìm user
            - DocumentShareRepository.ShareDocument(...)
            - reply success
    Tab 3: Danh sách collaborator
      → Server: HandleDocShareList → trả về list
      → Owner thay đổi permission → SocketClient.UpdatePermission
      → Owner thu hồi → SocketClient.RevokeAccess

[User được mời / có code]
  HomeForm: Nhập share code → btnJoin
    → SocketClient.JoinByCode(code)
       Server: HandleDocJoinCode
          - tìm doc theo share_code
          - DocumentShareRepository.ShareDocument(docId, currentUserId, "viewer") (nếu chưa)
          - reply doc info
    → Mở TypeRenderForm
```

### 🛡️ Sửa các handler hiện có
- `HandleDocOpen`: thay vì check `OwnerId == currentUserId`, gọi `DocumentShareRepository.GetPermission(docId, userId)`. Nếu null → lỗi.
- `HandleDocSave`: yêu cầu permission ∈ {owner, editor}.
- `HandleOpInsert/HandleOpDelete`: yêu cầu permission ∈ {owner, editor}.
- `HandleDocCreate`: sau khi tạo doc, sinh `shareCode` (8 ký tự `[A-Z0-9]`), update DB, đưa vào response.
- `HandleDocList`: query đã có JOIN shares → permission lấy từ share thay vì hardcode "viewer".

### 📂 Files chỉnh
- `Server/Database/Scripts/schema_init.sql` — ALTER TABLE thêm `share_code`
- `Server/Database/Models/Document.cs` — thêm `ShareCode`
- `Server/Database/Repositories/DocumentRepository.cs` — `UpdateShareCode`, `GetByShareCode`
- `Server/Network/ClientHandler.cs` — bổ sung 4 handler + sửa 4 handler cũ
- `Server/Services/ShareCodeGenerator.cs` (mới) — generate code 8 ký tự, retry nếu trùng
- `Shared/Packet.cs` — bổ sung MessageType + payload
- `Client/Network/SocketClient.cs` — 5 hàm mới
- `Client/ShareDocumentForm.cs` (mới) — dialog 3 tab
- `Client/HomeForm.cs` — thêm nút "Join by code"

### ✅ Checklist CN6 (10 mục)
- [x] ALTER TABLE thêm `share_code` + index unique
- [x] `Document` model + repo có `ShareCode`
- [x] `ShareCodeGenerator` random 8 ký tự A-Z0-9, retry trùng
- [x] `HandleDocCreate` tự sinh share code
- [x] Sửa `HandleDocOpen/Save/OpInsert/OpDelete` dùng permission
- [x] `HandleDocShare` invite by username + permission
- [x] `HandleDocJoinCode` join by code
- [x] `HandleDocShareList/Update/Revoke/RegenCode`
- [x] UI `ShareDocumentForm` (code + invite + list collaborator)
- [x] UI HomeForm nút "Tham gia mã"

---

# 7️⃣ LƯU ẢNH NGƯỜI DÙNG LÊN SERVER

### 🎯 Mục tiêu
- (a) **Avatar**: User có thể upload ảnh đại diện (1 ảnh/user).
- (b) **Inline image trong markdown**: User chèn ảnh vào doc → ảnh được upload lên server, server trả URL → markdown chèn `![alt](url)` → preview hiển thị.

### 📍 Hiện trạng
- ✅ Bảng `images` đã có schema (BYTEA + URL fallback)
- ✅ Bảng `users.avatar_url` đã có
- ❌ Chưa có Repository, Handler, packet

### 🧱 Phương án lưu trữ
- **Phương án A**: Lưu binary trong DB cột `BYTEA`.
  - Ưu: đơn giản, không cần file system.
  - Nhược: DB phình to, query chậm hơn.
- **Phương án B**: Lưu file vào thư mục `Server/Storage/images/{userId}/{guid}.png`, DB lưu URL.
  - Ưu: nhanh, dễ serve.
  - Nhược: phải xử lý file system, đường dẫn.

→ **Khuyến nghị**: Dùng **B** cho bài toán này. Server lưu file, packet gửi binary từ client lên, server lưu vào disk + ghi DB.

### 🧱 Packet
```csharp
IMAGE_UPLOAD,            // Client → Server
IMAGE_UPLOAD_RESPONSE,   // Server → Client (chứa URL)
IMAGE_AVATAR_SET,        // (gộp được vào upload với flag isAvatar)

public class Payload_IMAGE_UPLOAD_Request
{
    public string fileName { get; set; }
    public string mimeType { get; set; }
    public byte[] data { get; set; }       // Newtonsoft tự encode base64
    public bool isAvatar { get; set; }     // true → cập nhật users.avatar_url
    public string docID { get; set; }      // optional — gắn với doc
}

public class Payload_IMAGE_UPLOAD_Response
{
    public bool success { get; set; }
    public string message { get; set; }
    public string imageId { get; set; }
    public string url { get; set; }        // URL/path để chèn vào markdown
}
```

### ⚠️ Giới hạn kích thước
- Frame TCP 4-byte length → max 2GB nhưng RAM sẽ chết.
- **Bắt buộc** giới hạn `data.Length <= 5 MB` ở **cả client và server**.
- Client kiểm tra trước khi gửi, hiển thị MessageBox nếu vượt.

### 🔁 Flow
```
[Avatar]
  HomeForm: btnUpdateAvatar → OpenFileDialog
    → Đọc bytes + check size
    → SocketClient.UploadImage(fileName, mimeType, bytes, isAvatar=true)
       Server.HandleImageUpload
         - Validate kích thước, mime type
         - Tạo guid → lưu file vào Storage/images/{userId}/avatar_{guid}.{ext}
         - INSERT images
         - UPDATE users.avatar_url
         - Reply URL
    → HomeForm cập nhật picturebox

[Image inline]
  TypeRenderForm: btnInsertImage → OpenFileDialog
    → SocketClient.UploadImage(..., isAvatar=false, docID=...)
    → Nhận URL
    → Chèn `![alt](url)` vào txtRawMarkdown tại con trỏ
    → Render markdown sẽ tự hiển thị (Markdig + WebView2)
```

### 📌 Vấn đề: WebView2 truy cập file local thế nào?
- File lưu ở `Server/Storage/images/...` — Client không tự nhìn được.
- **Giải pháp 1**: Server cũng làm HTTP server tĩnh (khó với TCP server thuần). Bỏ qua.
- **Giải pháp 2**: Khi WebView2 cần ảnh → Client xin server qua packet `IMAGE_GET` → cache vào local temp folder → markdown trỏ tới `file:///`. Đơn giản nhất.
- **Giải pháp 3 (khuyên dùng cho phạm vi project)**: Khi upload xong, client lưu file vào local cache `%TEMP%/MarkTogether/{docId}/{imageId}.ext` → markdown chèn `file:///%TEMP%/...`. Server vẫn giữ bản gốc.

### 📂 Files chỉnh
- `Server/Database/Scripts/schema_init.sql` — đảm bảo bảng images đã có (đã có)
- `Server/Database/Repositories/ImageRepository.cs` (mới)
- `Server/Services/ImageStorageService.cs` (mới) — quản lý lưu/đọc file
- `Server/Network/ClientHandler.cs` — `HandleImageUpload`, `HandleImageGet`
- `Shared/Packet.cs` — packet
- `Client/Services/ImageCacheService.cs` (mới) — lưu cache, build URL
- `Client/Network/SocketClient.cs` — `UploadImage`, `DownloadImage`
- `Client/HomeForm.cs` — UI avatar
- `Client/TypeRenderForm.cs` — nút chèn ảnh

### ✅ Checklist CN7 (8 mục)
- [x] `ImageRepository` (Create/Get)
- [x] `ImageStorageService` quản lý folder Server/Storage
- [x] Packet `IMAGE_UPLOAD` + `IMAGE_GET`
- [x] Validate size <= 5 MB ở client + server
- [x] `HandleImageUpload` lưu file + DB + (avatar) update users
- [x] `HandleImageGet` trả binary cho client
- [x] Cache ảnh upload vào `%TEMP%/MarkTogether/{docId}/`
- [x] UI: avatar ở HomeForm + chèn ảnh ở TypeRenderForm

---

# 8️⃣ CHATBOT AI

### 🎯 Mục tiêu
Người dùng mở panel "AI Assistant" trong `TypeRenderForm`, gõ câu hỏi (ví dụ: "tóm tắt đoạn này", "giải thích thuật ngữ X", "viết tiếp đoạn"). Server gọi API ngoài (Gemini / OpenAI) và trả về.

### 🧠 Lựa chọn provider
| Provider | Free tier | Endpoint |
|----------|-----------|----------|
| **Google Gemini** | Có (rộng rãi) | `https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent` |
| **OpenAI** | Trả phí | `https://api.openai.com/v1/chat/completions` |

→ **Khuyến nghị Gemini** (free tier dễ).

### 🧱 Packet
```csharp
AI_REQUEST,
AI_RESPONSE,

public class Payload_AI_REQUEST
{
    public string docID { get; set; }       // optional context
    public string mode { get; set; }        // "chat" | "summarize" | "continue" | "translate"
    public string userPrompt { get; set; }
    public string contextText { get; set; } // đoạn được chọn (nếu có)
}

public class Payload_AI_RESPONSE
{
    public bool success { get; set; }
    public string message { get; set; }
    public string text { get; set; }        // câu trả lời
}
```

### 🔁 Flow
```
[Client]
  TypeRenderForm: panel AI
    → User gõ prompt (kèm tuỳ chọn "use selection as context")
    → SocketClient.AskAI(mode, prompt, context)
       Server.HandleAiRequest
         - Validate user
         - AISuggestionService.AskAsync(...)
            HttpClient POST tới Gemini API
            Return text
         - Reply Payload_AI_RESPONSE
  → Render trả lời vào panel chat AI
```

### 🔐 Lưu API key
- File `Server/App.config` thêm key `<add key="GeminiApiKey" value="..."/>`
- **Không commit** file này (đảm bảo `.gitignore`).
- Server đọc qua `ConfigurationManager.AppSettings["GeminiApiKey"]`.

### 📂 Files chỉnh
- `Server/Services/AISuggestionService.cs` (mới) — wrapper HTTP
- `Server/Network/ClientHandler.cs` — `HandleAiRequest`
- `Shared/Packet.cs` — packet
- `Client/Network/SocketClient.cs` — `AskAI`
- `Client/TypeRenderForm.cs/Designer.cs` — panel AI

### ⚠️ Lưu ý
- Gọi API là **async I/O** → server cần dùng `Task.Run` để không chặn client thread.
- Rate limit: thêm throttle 1 request / 3 giây / user.
- Timeout HTTP 30s, fallback message.
- Sanitize prompt (tránh user gửi prompt 100 KB → trả về quota tốn).

### ✅ Checklist CN8 (7 mục)
- [x] Slot key Gemini trong `App.config` (cần điền giá trị thật)
- [x] `AISuggestionService.AskAsync` gọi HTTP, parse response
- [x] Packet `AI_REQUEST/AI_RESPONSE`
- [x] `HandleAiRequest` async + throttle 1 req / 3 giây
- [x] `SocketClient.AskAI`
- [x] UI panel AI ở `TypeRenderForm` (tab AI: prompt + mode + history)
- [ ] Test các mode: chat / summarize / continue / translate — *cần API key thật*

---

# 9️⃣ LƯU LẠI VERSION TÀI LIỆU

### 🎯 Mục tiêu
- Tự động lưu version mỗi khi user save thủ công (nút Save) — không lưu mỗi op realtime để tránh spam.
- User có thể mở dialog "Version History" → xem danh sách → preview → khôi phục.

### 📍 Hiện trạng
- ✅ Bảng `document_versions` + `DocumentVersionRepository` đầy đủ
- ❌ Chưa được gọi từ flow nào

### 🧱 Packet
```csharp
DOC_VERSION_LIST,
DOC_VERSION_DETAIL,
DOC_VERSION_RESTORE,
DOC_VERSION_DELETE,

public class Payload_DOC_VERSION_LIST_Request
{
    public string docID { get; set; }
    public int page { get; set; } = 1;
    public int limit { get; set; } = 20;
}

public class VersionInfoDto
{
    public string id { get; set; }
    public DateTime savedAt { get; set; }
    public int savedByUserId { get; set; }
    public string savedByUsername { get; set; }
    public string label { get; set; }
}

public class Payload_DOC_VERSION_LIST_Response
{
    public string docID { get; set; }
    public int total { get; set; }
    public List<VersionInfoDto> versions { get; set; }
}

public class Payload_DOC_VERSION_DETAIL_Request
{
    public string versionId { get; set; }
}

public class Payload_DOC_VERSION_DETAIL_Response
{
    public string id { get; set; }
    public string docID { get; set; }
    public string contentSnapshot { get; set; }
    public DateTime savedAt { get; set; }
}

public class Payload_DOC_VERSION_RESTORE_Request
{
    public string docID { get; set; }
    public string versionId { get; set; }
}

public class Payload_DOC_VERSION_RESTORE_Response
{
    public bool success { get; set; }
    public string message { get; set; }
    public string newContent { get; set; }
}
```

### 🔁 Flow
```
[Auto-version on save]
  Client: bấm "Save" hoặc đóng form
    → SocketClient.SaveDocument(docId, content)
       Server: HandleDocSave
         - Update content
         - DocumentVersionRepository.SaveVersion(snapshot, savedBy, label="Manual save")
         - Trim version cũ nếu > 50 (tuỳ policy)

[View history]
  TypeRenderForm: btnVersionHistory → mở VersionHistoryForm
    → SocketClient.GetVersions(docId)
    → Hiển thị list version (saved at, by whom)
    → Click 1 version → SocketClient.GetVersionDetail(id) → preview readonly
    → Bấm "Restore" → confirm → SocketClient.RestoreVersion(docId, versionId)
       Server: HandleDocVersionRestore
         - Kiểm tra owner/editor
         - DocumentVersionRepository.RestoreVersion (đã có)
         - Lưu version mới ngay sau restore (label="Restored from {oldId}")
         - Broadcast OP_BROADCAST với content mới? Hoặc kick client mở lại doc.
```

### ⚠️ Đồng bộ với client đang mở
Khi A restore, các client khác đang sửa cần:
- Phương án đơn giản: gửi `DOC_RELOAD_BROADCAST` cho room → client tự fetch lại doc.
- Phương án OT: vượt phạm vi.

### 📂 Files chỉnh
- `Server/Network/ClientHandler.cs` — bổ sung lưu version trong `HandleDocSave` + 4 handler version
- `Shared/Packet.cs` — packet
- `Client/Network/SocketClient.cs` — 4 method
- `Client/VersionHistoryForm.cs` (mới) — list + preview + restore
- `Client/TypeRenderForm.cs` — nút "Version History"

### ✅ Checklist CN9 (9 mục)
- [x] `HandleDocSave` lưu version mỗi lần save
- [x] Policy giữ tối đa 50 version, xoá cũ (hàm `TrimVersions`)
- [x] Packet 4 MessageType + payload
- [x] `HandleVersionList/Detail/Restore/Delete`
- [x] Restore phải kiểm tra quyền (owner/editor)
- [x] Broadcast `DOC_RELOAD_BROADCAST` cho room sau restore
- [x] UI `VersionHistoryForm` (list + preview readonly + restore + delete)
- [ ] Diff view (optional) — so 2 version — *future work*
- [x] Nút mở từ `TypeRenderForm`

---

# 🗓️ THỨ TỰ TRIỂN KHAI ĐỀ XUẤT

> Triển khai theo chiều phụ thuộc. **Hạ tầng H1/H2/H3 phải làm trước** để mọi tính năng broadcast hoạt động.

| Bước | Ngày dự kiến | Việc | Ghi chú |
|------|--------------|------|---------|
| 1 | T+0 → T+2 | Hạ tầng **H1, H2, H3** | Critical path |
| 2 | T+2 → T+3 | **CN1** Logout | Nhanh, làm song song khi build H |
| 3 | T+3 → T+5 | **CN6** Set quyền chia sẻ | Phải làm trước CN4/CN5 vì nhiều handler check permission |
| 4 | T+5 → T+7 | **CN4** Chat trong phiên | Test broadcast pipeline |
| 5 | T+7 → T+9 | **CN9** Version | Tận dụng hook `HandleDocSave` |
| 6 | T+9 → T+11 | **CN3** Export PDF | Độc lập, làm tách |
| 7 | T+11 → T+14 | **CN5** Comment | Phức tạp UI nhất |
| 8 | T+14 → T+16 | **CN7** Image upload | Test với file >1MB |
| 9 | T+16 → T+18 | **CN8** Chatbot AI | Có khoá API là xong |

---

# 📚 PHỤ LỤC — TIPS KHI THỰC THI

### A. Khi thêm `MessageType` mới
1. Thêm vào `enum MessageType` trong `Shared/Packet.cs`.
2. Thêm class `Payload_*` ngay phía dưới (request + response).
3. Nếu là server-side: thêm `case` trong switch của `ClientHandler.ProcessAsync`.
4. Nếu là client-side broadcast: đăng ký handler ở dispatcher (H1).
5. Luôn thêm reply `OK`/`ERROR` rõ ràng.

### B. Khi thêm bảng DB mới
1. Thêm vào `Server/Database/Scripts/schema_init.sql` với `IF NOT EXISTS`.
2. Tạo Model trong `Server/Database/Models/`.
3. Tạo Repository static trong `Server/Database/Repositories/` với Dapper.
4. Tự chạy lại `schema_init.sql` để tạo bảng.
5. Migration: viết file SQL `migrations/2026-05-XX_add_xxx.sql` riêng nếu prod đã có dữ liệu.

### C. Quy ước commit (gợi ý)
- `feat(auth): logout endpoint + UI`
- `feat(chat): broadcast pipeline + chat panel`
- `fix(socket): correlation race condition`
- `chore(db): add document_comments table`

### D. Test thủ công cần có
- 2 client cùng login, cùng mở 1 doc → check chat / op broadcast / comment realtime.
- Owner share cho user B → user B mở được, sửa được/không tuỳ permission.
- Khôi phục version → 2 client đều nhận được nội dung mới.

### E. Bẫy thường gặp
- **`PacketHelper.Receive`** đang blocking → bắt buộc tách thread (H1).
- **Token timeout** chưa có → tạm chấp nhận, ghi chú vào Future Work.
- **WebView2 navigate** chỉ xong khi `NavigationCompleted` → chú ý race khi gọi `PrintToPdfAsync`.
- **JSON serialize byte[]** trong Newtonsoft mặc định → base64. Lưu ý decode đúng phía server.
- **Race khi nhiều thread cùng `Send` 1 stream** → bọc `lock(_streamLock)`.

---

# ✅ CHECKLIST TỔNG HỢP TICK NHANH

### Hạ tầng
- [x] H1. SocketClient async + correlation
- [x] H2. SessionManager DocRoom
- [x] H3. ClientHandler.SendSafe thread-safe

### CN1 — Đăng nhập / Đăng xuất
- [x] AUTH_LOGOUT MessageType
- [x] HandleLogout + LeaveAllRooms
- [x] SocketClient.Logout
- [x] btnLogout HomeForm + flow về LoginForm

### CN2 — Markdown render ✅
- [x] Đã hoàn thành

### CN3 — Export PDF
- [x] btnExportPdf
- [x] CSS @media print
- [x] PrintToPdfAsync flow + flush preview

### CN4 — Chat
- [x] CHAT_SEND/BROADCAST/HISTORY
- [x] Bảng chat_messages
- [x] JoinRoom/LeaveRoom hooks
- [x] HandleChat* + UI panel

### CN5 — Comment
- [x] Bảng document_comments
- [x] 5 MessageType
- [x] 4 handler + broadcast
- [x] Anchor snippet
- [x] UI panel + NewCommentForm

### CN6 — Quyền chia sẻ
- [x] ALTER documents add share_code
- [x] ShareCodeGenerator
- [x] HandleDocCreate sinh code
- [x] Sửa Open/Save/OpInsert/OpDelete dùng permission
- [x] HandleDocShare/JoinCode/List/Update/Revoke/RegenCode
- [x] ShareDocumentForm
- [x] HomeForm Join by code

### CN7 — Lưu ảnh
- [x] ImageRepository + StorageService
- [x] IMAGE_UPLOAD/GET
- [x] Validate size 5MB
- [x] Cache ảnh client vào %TEMP%
- [x] UI avatar + chèn ảnh

### CN8 — Chatbot AI
- [x] App.config GeminiApiKey (slot)
- [x] AISuggestionService HttpClient
- [x] AI_REQUEST/RESPONSE
- [x] Throttle 1req/3s
- [x] UI panel AI

### CN9 — Version
- [x] HandleDocSave lưu version
- [x] Trim 50 version
- [x] DOC_VERSION_LIST/DETAIL/RESTORE/DELETE
- [x] DOC_RELOAD_BROADCAST
- [x] VersionHistoryForm

---

> Cập nhật mỗi khi tick xong một mục. Khi mọi checklist được tick → MarkTogether đạt mức "Đầy đủ tính năng cốt lõi".