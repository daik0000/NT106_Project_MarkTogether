# Security T1-T3 Implementation Notes — 2026-05-22

## Phạm vi

Triển khai 3 task bắt buộc trong `Plan/SECURITY_IMPLEMENTATION_PLAN.md`:

- T1: Session token dùng CSPRNG.
- T2: Rate-limit login theo username.
- T3: TLS qua `SslStream`.

Không sửa OT engine, DB schema/migration, Packet/MessageType enum, UI Designer, logic AI/Markdown render.

---

## 1. Nguyên nhân

### T1 — Session token

`AuthService.GenerateToken()` dùng `Guid.NewGuid().ToString("N")`. GUID không được Microsoft document như CSPRNG security token chính danh.

### T2 — Login brute force

`AuthService.Login()` trước đó không có cooldown sau nhiều lần sai password. BCrypt đã làm chậm brute force, nhưng vẫn cần lockout nhẹ để giảm rủi ro password yếu.

### T3 — Plaintext TCP

`PacketHelper.Send/Receive` đọc/ghi trực tiếp `NetworkStream`, nên password, session token, nội dung tài liệu, chat và AI API key đi plaintext trên TCP.

---

## 2. Cách fix

### T1

File: `MarkTogether.Server/Services/AuthService.cs`

- `GenerateToken()` đổi sang:
  - `SecureTokenGenerator.GenerateUrlSafeToken(48)`

### T2

File: `MarkTogether.Server/Services/AuthService.cs`

- Thêm `ConcurrentDictionary<string, LoginFailEntry>` in-memory.
- Key: username trim + lower-case.
- Sai password:
  - tăng fail count,
  - từ lần sai thứ 5 khóa 15 phút,
  - log username/count, không log password.
- Login đúng:
  - clear counter của username.

### T3

Files:

- `MarkTogether.Shared/PacketHelper.cs`
  - API đổi từ `NetworkStream` sang `Stream`.
  - Lock per-stream đổi sang `ConditionalWeakTable<Stream, object>`.

- `MarkTogether.Server/Network/ClientHandler.cs`
  - `_stream` đổi sang `Stream`.
  - Constructor nhận `X509Certificate2`.
  - Wrap `TcpClient.GetStream()` bằng `SslStream`.
  - `AuthenticateAsServer(..., SslProtocols.Tls12, ...)`.

- `MarkTogether.Server/Network/SocketServer.cs`
  - Constructor nhận server certificate.
  - Pass certificate vào `ClientHandler`.

- `MarkTogether.Server/Program.cs`
  - Load PFX từ `App.config`:
    - `TlsCertPath`
    - `TlsCertPassword`
  - Khởi tạo `SocketServer(port, cert)`.

- `MarkTogether.Client/Network/SocketClient.cs`
  - `_stream` đổi sang `Stream`.
  - Sau TCP connect, wrap bằng `SslStream`.
  - `AuthenticateAsClient(..., SslProtocols.Tls12, ...)`.
  - Đọc `CERT_THUMB` từ `server.config`.
  - Nếu `CERT_THUMB` rỗng: dev/demo mode chấp nhận self-signed.
  - Nếu có `CERT_THUMB`: pin thumbprint bằng `certificate.GetCertHashString()`.

- `MarkTogether.Server/App.config`
  - Thêm:
    - `TlsCertPath`
    - `TlsCertPassword`

- `MarkTogether.Client/server.config`
  - Thêm:
    - `CERT_THUMB=`

---

## 3. Flow xử lý sau thay đổi

### Login flow

1. Client connect TCP tới server.
2. Client tạo `SslStream` và TLS handshake.
3. Server `ClientHandler` tạo `SslStream` và TLS handshake bằng PFX.
4. Sau handshake, mọi packet vẫn giữ format cũ:
   - 4-byte length prefix,
   - JSON `Packet`.
5. Client gửi `AUTH_LOGIN` qua TLS.
6. Server verify BCrypt.
7. Nếu đúng:
   - tạo token CSPRNG 48 ký tự,
   - add token vào `SessionManager`,
   - trả `AUTH_RESPONSE`.

### Login fail flow

1. Validate username/password không rỗng.
2. Kiểm tra lockout theo username lower-case.
3. Nếu đang bị khóa:
   - trả message `"Tài khoản tạm bị khoá..."`.
4. Nếu password sai:
   - tăng fail count,
   - lock nếu count >= 5.
5. Nếu login đúng:
   - xóa entry fail của username.

---

## 4. Lưu ý kỹ thuật

- Không thay đổi `Packet`, `MessageType`, payload schema.
- Không thay đổi framing protocol, chỉ thay transport stream.
- `SslStream` vẫn là `Stream`, nên `PacketHelper` dùng chung được cho client/server.
- `CERT_THUMB` để trống chỉ phù hợp dev/demo. Production nên cấu hình thumbprint.
- Cần copy PFX vào output server hoặc dùng absolute path trong `TlsCertPath`.
- `.gitignore` đã có `*.pfx`, không commit private key/cert file.

---

## 5. Thứ không được sửa lại

- Không revert `PacketHelper` về `NetworkStream`.
- Không dùng lại `Guid.NewGuid()` cho session token.
- Không log password, raw API key, full packet JSON.
- Không thêm DB migration cho login fail counter nếu chưa có yêu cầu riêng.
- Không tắt TLS để giữ backward plaintext compatibility; sau T3 client/server cùng dùng TLS.

---

## 6. Cách test

### Build

```powershell
$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
Start-Process -FilePath $msbuild -ArgumentList @("MarkTogether.sln","/p:Configuration=Debug","/t:Rebuild") -Wait -NoNewWindow
Start-Process -FilePath $msbuild -ArgumentList @("MarkTogether.sln","/p:Configuration=Release","/t:Rebuild") -Wait -NoNewWindow
```

### Tạo cert dev

```powershell
$cert = New-SelfSignedCertificate `
    -DnsName "marktogether.local","localhost" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(5) `
    -KeyAlgorithm RSA -KeyLength 2048

$pwd = ConvertTo-SecureString -String "ChangeMe-DEV" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "marktogether.pfx" -Password $pwd
$cert.Thumbprint
```

Copy `marktogether.pfx` vào `MarkTogether.Server/bin/Debug/` hoặc đổi `TlsCertPath` thành absolute path.

### Smoke test

1. Chạy server.
2. Chạy client.
3. Register/login.
4. Login sai 5 lần với cùng username, lần tiếp theo phải bị khóa.
5. Tạo/mở/lưu document.
6. Test 2 client cùng document, gõ realtime.
7. Nếu cấu hình `CERT_THUMB`, cố tình đổi thumbprint để xác nhận client từ chối cert.

---

## 7. Side effect cần chú ý

- Server không chạy được nếu thiếu `TlsCertPath` hoặc không tìm thấy PFX.
- Client cũ plaintext không kết nối được server mới TLS.
- Server cũ plaintext không kết nối được client mới TLS.
- Lockout login là in-memory, mất khi server restart.
- Token mới dài 48 ký tự thay vì 32 ký tự GUID, nhưng vẫn là `string` và không ảnh hưởng packet/session dictionary.