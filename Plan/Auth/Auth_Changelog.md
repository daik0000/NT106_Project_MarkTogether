# Auth System — Nhật Ký Thay Đổi

> **Ngày triển khai:** 12/04/2026  
> **Tổng cộng:** 10 file mới + 5 file sửa  
> **Build:** ✅ Thành công (MSBuild 18.4.0)

---

## 📁 MarkTogether.Shared

### [SỬA] `Packet.cs`
- **Thay đổi:** Thêm class `RegisterPayload` ở cuối file.
- **Lý do:** `AuthPayload` cũ chỉ có `Username` và `Password`, không đủ cho luồng đăng ký cần thêm trường `Email`.
- **Code thêm:**
```csharp
public class RegisterPayload
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}
```

---

## 📁 MarkTogether.Server

### [MỚI] `Services/AuthService.cs`
- **Vai trò:** Chứa logic nghiệp vụ xác thực (tách riêng khỏi tầng Network).
- **Hàm chính:**
  - `Register(username, email, password)` → Validate → Kiểm tra trùng username/email → BCrypt.HashPassword (workFactor=12) → Gọi `UserRepository.Create()` → Trả `AuthResponse`.
  - `Login(username, password)` → Tìm user theo username → BCrypt.Verify → Tạo token GUID → Trả `AuthResponse`.
  - `GenerateToken()` → Tạo GUID 32 ký tự hex.

### [MỚI] `Network/SessionManager.cs`
- **Vai trò:** Quản lý bảng mapping `Token ↔ UserId` trong RAM (không lưu DB).
- **Cấu trúc:** `ConcurrentDictionary<string, int>` (thread-safe).
- **Hàm chính:** `AddSession()`, `GetUserId()`, `RemoveSession()`, `IsValid()`.

### [MỚI] `Network/SocketServer.cs`
- **Vai trò:** TCP Socket Server lắng nghe trên port 5000.
- **Hoạt động:** Vòng lặp `AcceptTcpClientAsync()` → mỗi client được bàn giao cho 1 `ClientHandler` riêng chạy trên `Task.Run()`.
- **Hàm chính:** `StartAsync()`, `Stop()`.

### [MỚI] `Network/ClientHandler.cs`
- **Vai trò:** Xử lý từng client TCP. Mỗi client 1 instance riêng.
- **Hoạt động:** Vòng lặp đọc `Packet` → switch theo `MessageType` → gọi handler tương ứng.
- **Handler đã cài:**
  - `HandleRegister()` → Parse `RegisterPayload` → `AuthService.Register()` → Lưu session → Trả `AUTH_RESPONSE`.
  - `HandleLogin()` → Parse `AuthPayload` → `AuthService.Login()` → Lưu session → Trả `AUTH_RESPONSE`.
- **Xử lý disconnect:** Bắt `IOException` → xóa session → đóng stream/socket.

### [SỬA] `Program.cs`
- **Trước:** Rỗng (chỉ có `Main` trống).
- **Sau:** 
  - Bật `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true` (mapping snake_case ↔ PascalCase).
  - In banner server.
  - Khởi động `SocketServer(5000)` trên thread riêng.
  - `Console.ReadLine()` chờ Enter để dừng.

### [SỬA] `Server.csproj`
- **Thêm Compile Include:**
  - `Network\ClientHandler.cs`
  - `Network\SessionManager.cs`
  - `Network\SocketServer.cs`
  - `Services\AuthService.cs`
- **Sửa bug:** HintPath của Npgsql từ `net472` → `net461` (vì package Npgsql 4.1.13 không có thư mục net472).

---

## 📁 MarkTogether.Client

### [MỚI] `Network/SocketClient.cs`
- **Vai trò:** Singleton TCP client, giữ kết nối xuyên suốt phiên làm việc.
- **Thuộc tính lưu sau login:** `Token`, `UserId`, `Username`, `IsLoggedIn`.
- **Hàm chính:**
  - `Connect(host, port)` → Mở `TcpClient`.
  - `Login(username, password)` → Gửi `AUTH_LOGIN` packet → Nhận `AUTH_RESPONSE` → Lưu token.
  - `Register(username, email, password)` → Gửi `AUTH_REGISTER` packet → Nhận `AUTH_RESPONSE`.
  - `Disconnect()` → Đóng stream + socket.

### [MỚI] `LoginForm.cs` + `LoginForm.Designer.cs`
- **Giao diện:** Panel trắng trên nền xám, font Segoe UI.
  - `txtUsername` — Ô nhập username.
  - `txtPassword` — Ô nhập password (PasswordChar = '*').
  - `btnLogin` — Nút đăng nhập (màu xanh dương `#2196F3`).
  - `lnkRegister` — Link "Chưa có tài khoản? Đăng ký".
  - `lblError` — Label lỗi màu đỏ (ẩn mặc định).
- **Xử lý sự kiện:**
  - `btnLogin_Click` → Connect → Login → Nếu OK: ẩn LoginForm, mở Form1 / Nếu fail: hiện lỗi.
  - `lnkRegister_LinkClicked` → Mở `RegisterForm` dạng dialog.
  - `txtPassword_KeyDown` → Nhấn Enter = click nút Login.
  - `txtUsername_KeyDown` → Nhấn Enter = chuyển focus sang ô Password.

### [MỚI] `RegisterForm.cs` + `RegisterForm.Designer.cs`
- **Giao diện:** Tương tự LoginForm nhưng theme xanh lá `#4CAF50`.
  - `txtUsername`, `txtEmail`, `txtPassword`, `txtConfirmPassword`.
  - `btnRegister` — Nút đăng ký.
  - `lnkBackToLogin` — Link "Đã có tài khoản? Quay lại đăng nhập".
- **Validation phía client:**
  - Username + Password không được trống.
  - Password tối thiểu 6 ký tự.
  - Confirm password phải khớp.
- **Xử lý sự kiện:**
  - `btnRegister_Click` → Connect → Register → Nếu OK: đóng form / Nếu fail: hiện lỗi.
  - `txtConfirmPassword_KeyDown` → Nhấn Enter = click nút Đăng ký.

### [SỬA] `Program.cs`
- **Trước:** `Application.Run(new Form1());`
- **Sau:** `Application.Run(new LoginForm());`

### [SỬA] `Client.csproj`
- **Thêm Compile Include:**
  - `LoginForm.cs` (SubType: Form)
  - `LoginForm.Designer.cs` (DependentUpon: LoginForm.cs)
  - `RegisterForm.cs` (SubType: Form)
  - `RegisterForm.Designer.cs` (DependentUpon: RegisterForm.cs)
  - `Network\SocketClient.cs`

---

## 🔧 Sửa lỗi phát sinh khi Build

| Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|
| `Newtonsoft.Json` not found | Thư mục `packages/` chưa tồn tại (NuGet chưa restore) | Tải `nuget.exe` → chạy `nuget restore` |
| `Npgsql` not found | HintPath trỏ tới `net472` nhưng package chỉ có `net461` | Sửa HintPath trong `Server.csproj` |

---

## Cách Test End-to-End

```
1. Bật Docker PostgreSQL:
   docker start QTM

2. Nạp schema (lần đầu):
   psql -h localhost -p 5432 -U postgres -d myapp_db -f schema_init.sql

3. Chạy Server:
   MarkTogether.Server.exe
   → Console hiện: "Đang lắng nghe trên port 5000..."

4. Chạy Client:
   MarkTogether.Client.exe
   → Mở LoginForm

5. Bấm "Đăng ký" → Nhập username + password → Submit
   → Server log: "[Auth] Đăng ký thành công: test_user (ID=1)"

6. Quay lại LoginForm → Nhập username + password → Đăng nhập
   → Server log: "[Auth] Đăng nhập thành công: test_user (ID=1)"
   → Client mở Form1 (MainForm)
```
