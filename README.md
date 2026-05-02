# MarkTogether

> Đồ án môn **NT106 — Lập trình mạng căn bản**  
> Trường Đại học Công nghệ Thông tin 

---

## Giới thiệu

**MarkTogether** là ứng dụng soạn thảo Markdown cộng tác thời gian thực theo mô hình Client–Server. Nhiều người dùng có thể cùng chỉnh sửa một tài liệu đồng thời, với các thay đổi được đồng bộ tức thì qua kết nối TCP Socket — tương tự Google Docs nhưng được xây dựng từ nền tảng.

---

## Thành viên nhóm

| STT | Họ và tên | MSSV |
|-----|-----------|------|
| 1 | Đào Đình An | 24520041 |
| 2 | Nguyễn Thị Mỹ Duyên | 24520408 |
| 3 | Nguyễn Văn Quốc Gia | 24520415 |

---

## Công nghệ sử dụng

| Thành phần | Công nghệ |
|------------|-----------|
| Ngôn ngữ | C# (.NET Framework 4.7.2) |
| Giao diện Client | Windows Forms (WinForms) |
| Giao tiếp mạng | TCP Socket (port 5000) |
| Giao thức | Custom binary protocol — 4-byte length prefix + JSON payload |
| Cơ sở dữ liệu | PostgreSQL + Dapper ORM |
| Render Markdown | Markdig + WebView2 |
| Thuật toán đồng bộ | Operational Transformation (OT) |

---

## Kiến trúc hệ thống

```
┌─────────────────────┐        TCP Socket        ┌─────────────────────┐
│   MarkTogether      │   4-byte len + JSON PKT  │   MarkTogether      │
│   Client (WinForms) │ ◄──────────────────────► │   Server            │
│                     │                          │                     │
│  - LoginForm        │                          │  - SocketServer     │
│  - RegisterForm     │                          │  - ClientHandler    │
│  - HomeForm         │                          │  - OT Engine        │
│  - TypeRenderForm   │                          │  - SessionManager   │
│  - SocketClient     │                          │  - PostgreSQL       │
└─────────────────────┘                          └─────────────────────┘
```

**Luồng xử lý packet:**
1. Client đóng gói dữ liệu thành `Packet` (JSON) với `MessageType` tương ứng.
2. Gửi qua TCP: 4 byte đầu báo kích thước, theo sau là JSON.
3. Server đọc, dispatch theo `MessageType`, xử lý, trả response.
4. Với các thao tác chỉnh sửa, server transform qua OT Engine rồi broadcast cho các client khác.

---

## Tính năng đã hoàn thiện

### Xác thực tài khoản
- Đăng ký tài khoản mới (username, email, mật khẩu).
- Đăng nhập và nhận session token.
- Quản lý session trong RAM qua `SessionManager`.

### Quản lý tài liệu
- Tạo tài liệu Markdown mới với tiêu đề tùy chỉnh.
- Import file `.md` từ máy tính lên server.
- Xem danh sách tài liệu với sắp xếp theo: Mới → Cũ, Cũ → Mới, A–Z, Z–A.
- Mở và chỉnh sửa tài liệu đã có.
- Tự động lưu khi đóng cửa sổ soạn thảo.

### Chia sẻ và cộng tác
- Chia sẻ tài liệu với người dùng khác theo username → tạo mã chia sẻ (share code).
- Tham gia tài liệu bằng mã chia sẻ (Join by Code).
- Phân quyền: `owner`, `editor`, `viewer`.
  - `owner` và `editor`: đọc + sửa + lưu.
  - `viewer`: chỉ đọc.
- Gửi `DOC_LEAVE` khi người dùng rời tài liệu.

### Soạn thảo Markdown thời gian thực
- Editor chia đôi màn hình: raw Markdown bên trái, preview HTML bên phải.
- Preview render tức thì qua WebView2 với debounce 280ms.
- Hỗ trợ syntax highlighting (highlight.js), bảng, blockquote, code block.

### Đồng bộ cộng tác — Operational Transformation (OT)
- Client gửi thao tác `OP_INSERT` / `OP_DELETE` với debounce 400ms.
- Server transform operation của client against các operation đã apply đồng thời (dựa trên `clientRevision`).
- Quy tắc transform đầy đủ: Insert↔Insert, Insert↔Delete, Delete↔Insert, Delete↔Delete.
- Lưu toàn bộ operation history vào bảng `document_operations` (revision tăng dần).
- Broadcast operation đã transformed cho các client khác đang cùng mở tài liệu.
- Client nhận `OP_BROADCAST`, áp dụng vào editor giữ nguyên vị trí con trỏ.
- Quản lý trạng thái in-memory per document qua `DocumentStateManager`.

---

## Cấu trúc project

```
NT106_Project_MarkTogether/
├── MarkTogether.Shared/          # Giao thức dùng chung
│   ├── Packet.cs                 # Định nghĩa Packet + tất cả Payload models
│   └── PacketHelper.cs           # Send/Receive TCP với length-prefix
│
├── MarkTogether.Client/          # Ứng dụng WinForms
│   ├── LoginForm.cs
│   ├── RegisterForm.cs
│   ├── HomeForm.cs               # Danh sách tài liệu, Share, Join Code
│   ├── TypeRenderForm.cs         # Editor + Preview + OT client
│   ├── CreateDocumentForm.cs
│   └── Network/
│       └── SocketClient.cs       # Singleton TCP client + background listener
│
└── MarkTogether.Server/          # Console server
    ├── Program.cs
    ├── Network/
    │   ├── SocketServer.cs       # Lắng nghe TCP, quản lý handlers
    │   ├── ClientHandler.cs      # Xử lý packet từng client
    │   └── SessionManager.cs     # Quản lý token session
    ├── OT/
    │   ├── OTEngine.cs           # Thuật toán Operational Transformation
    │   ├── DocumentState.cs      # Trạng thái + lock per document
    │   └── DocumentStateManager.cs
    ├── Services/
    │   └── AuthService.cs
    └── Database/
        ├── DbConnectionFactory.cs
        ├── Models/               # Document, User, DocumentOperation, ...
        └── Repositories/         # DocumentRepository, UserRepository, ...
```

---

## Cơ sở dữ liệu

**PostgreSQL** với các bảng chính:

| Bảng | Mô tả |
|------|-------|
| `users` | Tài khoản người dùng |
| `documents` | Tài liệu (owner, title, content, share_code) |
| `document_shares` | Phân quyền chia sẻ (doc_id, user_id, permission) |
| `document_operations` | Lịch sử thao tác OT (op_type, pos, text, revision) |
| `document_versions` | Phiên bản snapshot tài liệu |

---

## Hướng dẫn chạy

### Yêu cầu
- Visual Studio 2022 trở lên
- .NET Framework 4.7.2
- PostgreSQL 13+
- WebView2 Runtime

### Cấu hình database
1. Tạo database `myapp_db` trong PostgreSQL.
2. Chạy script tạo bảng (xem file `schema.sql` nếu có).
3. Thêm cột share_code nếu chưa có:
   ```sql
   ALTER TABLE documents ADD COLUMN IF NOT EXISTS share_code VARCHAR(20) UNIQUE;
   CREATE INDEX IF NOT EXISTS idx_doc_ops_doc_revision
       ON document_operations(doc_id, revision);
   ```
4. Cập nhật connection string trong `MarkTogether.Server/App.config`:
   ```xml
   <add name="MarkTogetherDb"
        connectionString="Host=localhost;Port=5432;Database=myapp_db;Username=postgres;Password=YOUR_PASSWORD"
        providerName="Npgsql" />
   ```

### Build
```cmd
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" MarkTogether.sln /p:Configuration=Debug /t:Rebuild
```

### Chạy
Mở 2 terminal riêng, chạy Server trước:

```cmd
:: Terminal 1 — Server
MarkTogether.Server\bin\Debug\MarkTogether.Server.exe

:: Terminal 2 — Client
MarkTogether.Client\bin\Debug\MarkTogether.Client.exe
```

---

## Giao thức mạng

Mọi dữ liệu trao đổi đều dùng class `Packet`:

```csharp
public class Packet {
    public MessageType Type { get; set; }  // Loại message
    public string Token { get; set; }      // Session token
    public string Payload { get; set; }    // JSON payload
}
```

Các `MessageType` đã implement:

| MessageType | Chiều | Mô tả |
|-------------|-------|-------|
| `AUTH_REGISTER` | C→S | Đăng ký tài khoản |
| `AUTH_LOGIN` | C→S | Đăng nhập |
| `AUTH_RESPONSE` | S→C | Kết quả xác thực + token |
| `DOC_CREATE` | C↔S | Tạo tài liệu mới |
| `DOC_LIST` | C↔S | Lấy danh sách tài liệu |
| `DOC_OPEN` | C↔S | Mở tài liệu |
| `DOC_SAVE` | C→S | Lưu tài liệu |
| `DOC_SHARE` | C↔S | Chia sẻ tài liệu |
| `DOC_JOIN_CODE` | C↔S | Tham gia bằng mã |
| `DOC_LEAVE` | C→S | Rời tài liệu |
| `OP_INSERT` | C→S | Thao tác chèn ký tự |
| `OP_DELETE` | C→S | Thao tác xóa ký tự |
| `OP_BROADCAST` | S→C | Broadcast thao tác đã transform |
| `OK` | S→C | Thành công |
| `ERROR` | S→C | Lỗi kèm thông báo |

---

## Thuật toán Operational Transformation

OT giải quyết vấn đề xung đột khi nhiều người chỉnh sửa đồng thời:

```
User A gõ "X" tại pos 2  ──┐
                           ├──► Server transform ──► broadcast đúng vị trí
User B xóa tại pos 3     ──┘
```

**Quy tắc transform được implement:**

- **Insert vs Insert**: nếu op2 xảy ra trước hoặc tại vị trí op1 → dịch pos của op1 sang phải.
- **Insert vs Delete**: nếu delete hoàn toàn trước op1 → lùi pos; nếu overlap → snap về vị trí delete.
- **Delete vs Insert**: nếu insert trước vị trí delete → dịch pos sang phải.
- **Delete vs Delete**: tính phần overlap, trim text bị xóa cho phù hợp.

---

## Git Branches

| Branch | Mô tả |
|--------|-------|
| `master` | Nhánh ổn định ban đầu |
| `features/type_and_render` | Tính năng soạn thảo + preview Markdown |
| `features/collab-realtime` | Cộng tác thời gian thực + OT |