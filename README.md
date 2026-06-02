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

## Hướng dẫn sử dụng (Dành cho người dùng)

Hệ thống **MarkTogether Server** đã được triển khai sẵn trên máy chủ VPS cùng với cơ sở dữ liệu PostgreSQL. Bạn chỉ cần chạy Client để kết nối và sử dụng.

### Yêu cầu
- Máy tính chạy Windows.
- Đã cài đặt **WebView2 Runtime** (thường đã có sẵn trên Windows 11).

### Cài đặt và kết nối
1. Tải về bản Release mới nhất của `MarkTogether.Client` hoặc build project `MarkTogether.Client`.
2. Mở thư mục chứa ứng dụng Client (ví dụ: `bin\Release`).
3. Mở file `server.config` bằng Notepad và nhập địa chỉ IP VPS của nhóm:
   ```text
   HOST=DIEN_IP_VPS_CUA_NHOM_VAO_DAY
   PORT=5000
   ```
   *(Lưu ý: Nếu chạy nội bộ trên cùng máy, giữ nguyên `HOST=localhost`)*
4. Chạy file `MarkTogether.Client.exe` để bắt đầu sử dụng. Bạn có thể mở nhiều cửa sổ Client cùng lúc để test tính năng cộng tác.

---

## Dành cho nhà phát triển (Chạy Server nội bộ)

Nếu bạn muốn phát triển thêm tính năng hoặc tự host server nội bộ, hãy làm theo các bước sau:

### Yêu cầu hệ thống
- Visual Studio 2022 trở lên.
- .NET Framework 4.7.2.
- PostgreSQL 13+.

### 1. Cấu hình Database
Nếu bạn chưa bao giờ tạo database cho dự án:

1.  **Cài đặt PostgreSQL:** Đảm bảo bạn đã cài đặt PostgreSQL và công cụ quản lý như **pgAdmin 4** hoặc **DBeaver**.
2.  **Tạo Database mới:**
    - Kết nối tới server PostgreSQL của bạn (mặc định localhost:5432).
    - Tạo database mới tên là `myapp_db`.
3.  **Khởi tạo Schema:**
    - Mở file script tại: `MarkTogether.Server/Database/Scripts/schema_init.sql`.
    - Chạy toàn bộ lệnh SQL trong file này trên database `myapp_db` để tạo các bảng, trigger và index cần thiết.
4.  **Cập nhật Chuỗi kết nối:**
    - Mở file `MarkTogether.Server/App.config`.
    - Cập nhật `Username` và `Password` đúng với PostgreSQL của bạn:
   ```xml
   <add name="MarkTogetherDb"
        connectionString="Host=localhost;Port=5432;Database=myapp_db;Username=postgres;Password=YOUR_PASSWORD"
        providerName="Npgsql" />
   ```

### 2. Triển khai Server lên Linux (VPS)
Dự án hỗ trợ chạy ngầm trên Linux thông qua Mono và Systemd:
- Cài đặt Mono: `sudo apt install mono-complete`.
- Chạy server dạng background service:
  ```bash
  mono MarkTogether.Server.exe --service
  ```

### 3. Build & Chạy nội bộ
```cmd
:: Build toàn bộ project
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" MarkTogether.sln /p:Configuration=Debug /t:Rebuild

:: Chạy Server
MarkTogether.Server\bin\Debug\MarkTogether.Server.exe

:: Chạy Client
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


## Cryptography & Security 

### Password Hashing
- **Algorithm**: BCrypt
- **Work Factor**: 12 (adaptive cost factor)
- **Purpose**: Bcrypt tự động salt và hash password, cung cấp bảo vệ tốt nhất chống brute-force attacks
- **Implementation**: Sử dụng thư viện `BCrypt.Net-Next v4.1.0`

### Data Integrity
- **Algorithm**: SHA-256
- **Usage**: Hash cho hình ảnh (image verification)
- **Implementation**: `System.Security.Cryptography.SHA256`

### Transport Security
- **Protocol**: TLS 1.2+
- **Certificate**: Self-signed certificates cho LAN/WAN demo
- **Purpose**: Mã hóa dữ liệu truyền giữa client-server và server-load-balancer

### Authentication Token
- **Generator**: `SecureTokenGenerator` - dùng `System.Security.Cryptography.RNGCryptoServiceProvider`
- **Purpose**: Tạo token ngẫu nhiên an toàn cho session quản lý

### Share Code Generation
- **Generator**: `ShareCodeGenerator` - dùng cryptographic randomness
- **Purpose**: Tạo mã chia sẻ document an toàn và không dự đoán được

---

## LAN/WAN Demo Setup 

### Architecture
```
Client A/B (LAN hoặc Internet)
        |
        | TLS, port 5000
        v
Load Balancer (0.0.0.0:5000)
        |
        | TLS upstream
        +--> App Server #1 (127.0.0.1:5101)
        +--> App Server #2 (127.0.0.1:5102)
              |
              +--> PostgreSQL (127.0.0.1:15432)
              +--> Redis (127.0.0.1:16379)
```

### Deployment Options

#### 1. LAN Demo (Local Network)
- **Load Balancer**: Công khai trên LAN (0.0.0.0:5000)
- **Backend Services**: Chỉ bind local (127.0.0.1)
- **Client Connection**: Dùng IP LAN của máy chạy LB (ví dụ: `192.168.1.10:5000`)
- **Certificates**: Self-signed, tạo bằng OpenSSL

**Hướng dẫn LAN Demo:**
- Xem chi tiết tại [deploy/LAN_DEMO.md](deploy/LAN_DEMO.md)
- Chạy script PowerShell tự động
- Hỗ trợ load balancing giữa 2 app servers

#### 2. WAN Demo (Internet)
- **Load Balancer**: Công khai trên Internet (0.0.0.0:5000)
- **Certificate**: Cấp từ Let's Encrypt hoặc CA tin cậy
- **Client Connection**: Dùng domain name hoặc public IP
- **Security**: Firewall rules để chỉ cho phép ports 5000 (app) và 5432 (nếu cần)

#### 3. Production Deployment
- **Reverse Proxy**: Nginx hoặc Azure Application Gateway
- **Load Balancing**: Kubernetes, Azure App Service, hoặc Docker Swarm
- **Database**: Managed PostgreSQL (Azure Database for PostgreSQL)
- **Cache**: Azure Cache for Redis
- **Certificates**: Tự động renewal từ Let's Encrypt qua reverse proxy
- **Monitoring**: Application Insights, Prometheus/Grafana

### Configuration Files
- **LAN Demo Environment**: `deploy/env/lan-demo.*.env.example`
- **Production Environment**: `deploy/env/marktogether.env.example`
- **Client Config**: `MarkTogether.Client/server.config.lan.example`
- **Docker Compose**: `docker-compose.lan-demo.yml`

### Build & Run
```powershell
# Build Release
dotnet build MarkTogether.sln -c Release /m:1

# LAN Demo (Windows PowerShell)
./deploy/scripts/lan-demo-start-db.ps1
./deploy/scripts/lan-demo-create-certs.ps1
./deploy/scripts/lan-demo-lb.ps1
./deploy/scripts/lan-demo-server1.ps1
./deploy/scripts/lan-demo-server2.ps1
```