# Debug SMTP Forgot Password MarkTogether trên VPS — Gmail bị chặn, chuyển Brevo

> Thời gian ghi nhận: 22/05/2026  
> Phạm vi: Forgot Password / Reset Password OTP qua SMTP trong `MarkTogether.Server`  
> File runtime liên quan:
>
> - `MarkTogether.Server/Services/AuthService.cs`
> - `MarkTogether.Server/Services/EmailService.cs`
> - `MarkTogether.Server/App.config`
> - `MarkTogether.Server/bin/Release/MarkTogether.Server.exe.config`
> - `deploy/scripts/check-smtp-diagnostics.sh`
> - `DOCUMENTATION.md`

---

## 1. Hiện tượng ban đầu

Chức năng **Forgot Password** bị treo hoặc timeout khi server cố gửi email OTP bằng Gmail SMTP.

Diagnostic trên VPS cho thấy DNS hoạt động:

```bash
getent hosts smtp.gmail.com
```

Nhưng kết nối SMTP Gmail timeout:

```bash
timeout 10 nc -4 -vz smtp.gmail.com 587
timeout 10 nc -4 -vz smtp.gmail.com 465
openssl s_client -starttls smtp -connect smtp.gmail.com:587
```

Firewall local không chặn outbound:

```bash
-P OUTPUT ACCEPT
```

Kết luận: VPS/provider đang block outbound SMTP chuẩn của Gmail trên `465/587`.

---

## 2. Xác minh đường SMTP thay thế

Test SMTP Brevo port `2525`:

```bash
timeout 10 nc -4 -vz smtp-relay.brevo.com 2525
```

Kết quả:

```text
Connection to smtp-relay.brevo.com 2525 succeeded
```

Suy ra VPS vẫn có internet bình thường, chỉ bị chặn SMTP Gmail/port chuẩn; port `2525` đi được.

---

## 3. Cách gửi mail hiện tại của MarkTogether

MarkTogether **không dùng** `sendmail`, `mailx`, `msmtp`, `curl` hay external CLI để gửi mail.

Code gửi mail bằng API có sẵn của .NET Framework:

```csharp
System.Net.Mail.SmtpClient
```

File gửi mail:

```text
MarkTogether.Server/Services/EmailService.cs
```

Flow:

```text
ForgotPasswordForm
  -> SocketClient.RequestPasswordReset(email)
  -> AUTH_FORGOT_PASSWORD
  -> ClientHandler.HandleForgotPassword
  -> AuthService.RequestPasswordResetAsync(email)
  -> EmailService.SendOtpAsync(user.Email, user.Username, otp)
  -> SmtpClient.SendMailAsync(...)
```

---

## 4. Chuyển cấu hình từ Gmail SMTP sang Brevo SMTP relay

Cấu hình ban đầu dùng Gmail:

```xml
<add key="SmtpHost" value="smtp.gmail.com" />
<add key="SmtpPort" value="587" />
<add key="SmtpUser" value="your_system_email@gmail.com" />
<add key="SmtpPassword" value="GMAIL_APP_PASSWORD" />
<add key="SmtpEnableSsl" value="true" />
```

Đã chuyển sang Brevo:

```xml
<add key="SmtpHost" value="smtp-relay.brevo.com" />
<add key="SmtpPort" value="2525" />
<add key="SmtpUser" value="YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com" />
<add key="SmtpPassword" value="BREVO_SMTP_KEY" />
<add key="SmtpFromEmail" value="your_verified_sender@example.com" />
<add key="SmtpFromName" value="MarkTogether" />
<add key="SmtpEnableSsl" value="true" />
```

File đã sửa:

```text
MarkTogether.Server/App.config
MarkTogether.Server/bin/Release/MarkTogether.Server.exe.config
```

---

## 5. Lỗi 535 Authentication failed và nguyên nhân

Sau lần đổi đầu tiên, log VPS báo:

```text
[Auth] SMTP send failed user=20: 535 5.7.8 Authentication failed
```

Ý nghĩa:

- VPS đã connect được Brevo.
- STARTTLS/SMTP không còn timeout.
- Brevo từ chối credential.
- Lỗi không còn là firewall/provider block port nữa.

Nguyên nhân là credential ban đầu chưa đúng chuẩn Brevo SMTP. Brevo yêu cầu SMTP login dạng:

```text
YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com
```

và SMTP key lấy trong Brevo Dashboard, không phải Gmail address + app password.

---

## 6. Test `swaks` thành công trên VPS

Người dùng test trực tiếp bằng `swaks`:

```bash
swaks --to recipient@example.com \
  --from your_verified_sender@example.com \
  --server smtp-relay.brevo.com \
  --port 2525 \
  --auth LOGIN \
  --auth-user YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com \
  --auth-password 'BREVO_SMTP_KEY' \
  --tls
```

Kết quả quan trọng:

```text
235 2.0.0 Authentication succeeded
250 2.0.0 Roger, accepting mail from <your_verified_sender@example.com>
250 2.0.0 I'll make sure <recipient@example.com> gets this
250 2.0.0 OK: queued as <20260522160105.116184@server-1>
```

Kết luận:

- Brevo credential đúng.
- Port `2525` hoạt động.
- STARTTLS hoạt động.
- From sender đã verify được Brevo chấp nhận.
- Mail test queue thành công.

---

## 7. Vì sao `swaks` thấy mail nhưng app báo gửi thành công mà inbox chưa thấy?

Sau khi sửa credential trên VPS, service log:

```text
[Auth] Reset OTP sent user=20 email=da***@gmail.com
```

Log này nghĩa là `SmtpClient.SendMailAsync(...)` **không throw exception**; SMTP server đã nhận message ở mức protocol.

Tuy nhiên lúc đó app và `swaks` chưa giống nhau hoàn toàn:

### `swaks` dùng

```bash
--auth-user YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com
--from your_verified_sender@example.com
```

### App ban đầu dùng

```csharp
new MailMessage(new MailAddress(user, fromName), new MailAddress(toEmail))
```

Trong đó `user = SmtpUser`.

Nếu `SmtpUser=YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com`, app sẽ gửi `From` là:

```text
YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com
```

khác với `swaks`:

```text
your_verified_sender@example.com
```

Do đó Brevo có thể accept SMTP transaction nhưng delivery/filtering khác nhau, đặc biệt với Gmail inbox.

---

## 8. Fix code: tách SMTP login và From email

Đã sửa `EmailService.cs` để tách:

- `SmtpUser`: dùng để login SMTP.
- `SmtpPassword`: SMTP key.
- `SmtpFromEmail`: địa chỉ From thật.
- `SmtpFromName`: tên hiển thị.

Code mới:

```csharp
string user = ConfigurationManager.AppSettings["SmtpUser"];
string pass = ConfigurationManager.AppSettings["SmtpPassword"];
string fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"];
string fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "MarkTogether";
```

Fallback nếu chưa cấu hình `SmtpFromEmail`:

```csharp
if (string.IsNullOrWhiteSpace(fromEmail))
    fromEmail = user;
```

Gửi mail:

```csharp
client.Credentials = new NetworkCredential(user, pass);

using (var msg = new MailMessage(new MailAddress(fromEmail, fromName), new MailAddress(toEmail)))
{
    ...
    await client.SendMailAsync(msg).ConfigureAwait(false);
}
```

Như vậy app giờ tương đương `swaks`:

```bash
--auth-user YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com
--from your_verified_sender@example.com
```

---

## 9. Luồng xác định user khi đặt lại mật khẩu

Câu hỏi: “Hiện tại đặt lại mật khẩu là cho user nào?”

Câu trả lời: user được xác định bằng **email nhập ở form Forgot Password**.

Trong `AuthService.RequestPasswordResetAsync(email)`:

```csharp
string key = email.Trim().ToLowerInvariant();
var user = UserRepository.GetByEmail(key);
```

Nếu tìm thấy user, server sinh OTP và lưu OTP kèm `UserId`:

```csharp
_resetOtps[key] = new OtpEntry
{
    OtpHash = Sha256(otp),
    ExpiresAtUtc = now.Add(OtpLifetime),
    CreatedAtUtc = now,
    UserId = user.Id
};
```

Khi reset:

```csharp
string key = email.Trim().ToLowerInvariant();

if (!_resetOtps.TryGetValue(key, out entry))
{
    return ...;
}
```

Nếu OTP đúng:

```csharp
UserRepository.UpdatePassword(entry.UserId, hash)
```

Vì vậy user bị đổi mật khẩu là:

```text
user có email = email nhập ở Forgot Password
```

Ví dụ log:

```text
[Auth] Reset OTP sent user=20 email=da***@gmail.com
```

nghĩa là OTP đó gắn với user ID `20`, email dạng `da***@gmail.com`. Nếu reset thành công sẽ log:

```text
[Auth] Password reset success user=20
```

---

## 10. File đã chỉnh

### `MarkTogether.Server/Services/EmailService.cs`

- Sử dụng `System.Net.Mail.SmtpClient`.
- Port fallback `2525`.
- `UseDefaultCredentials=false`.
- `DeliveryMethod=Network`.
- `Timeout=15000`.
- Thêm `SmtpFromEmail`.

### `MarkTogether.Server/App.config`

Thêm/cập nhật:

```xml
<add key="SmtpHost" value="smtp-relay.brevo.com" />
<add key="SmtpPort" value="2525" />
<add key="SmtpUser" value="YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com" />
<add key="SmtpPassword" value="BREVO_SMTP_KEY" />
<add key="SmtpFromEmail" value="your_verified_sender@example.com" />
<add key="SmtpFromName" value="MarkTogether" />
<add key="SmtpEnableSsl" value="true" />
```

### `MarkTogether.Server/bin/Release/MarkTogether.Server.exe.config`

Đồng bộ cấu hình Release để deploy VPS copy artifact dùng đúng config.

### `deploy/scripts/check-smtp-diagnostics.sh`

- Default SMTP target đổi sang `smtp-relay.brevo.com:2525`.
- Message diễn giải lỗi auth đổi từ Gmail App Password sang Brevo SMTP key / sender verification.

### `DOCUMENTATION.md`

- Cập nhật tổng quan Forgot Password dùng Brevo SMTP OTP thay Gmail SMTP.
- Ghi rõ `SmtpFromEmail`.

---

## 11. Build/verify

Đã build server:

```cmd
dotnet build MarkTogether.Server\Server.csproj -c Release
```

Kết quả:

```text
Build succeeded
```

Đọc lại Release config sau build:

```xml
<add key="SmtpHost" value="smtp-relay.brevo.com" />
<add key="SmtpPort" value="2525" />
<add key="SmtpUser" value="YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com" />
<add key="SmtpPassword" value="BREVO_SMTP_KEY" />
<add key="SmtpFromEmail" value="your_verified_sender@example.com" />
<add key="SmtpFromName" value="MarkTogether" />
<add key="SmtpEnableSsl" value="true" />
```

---

## 12. Checklist deploy lại VPS

Sau khi build local, cần đưa artifact mới lên VPS:

```bash
sudo systemctl stop marktogether
sudo cp -r MarkTogether.Server/bin/Release/* /opt/marktogether/server/
sudo chown -R marktogether:marktogether /opt/marktogether/server
sudo systemctl start marktogether
sudo journalctl -u marktogether -f
```

Kiểm tra config thật đang chạy:

```bash
grep -n 'Smtp' /opt/marktogether/server/MarkTogether.Server.exe.config
```

Kỳ vọng có:

```xml
<add key="SmtpUser" value="YOUR_BREVO_SMTP_LOGIN@smtp-brevo.com" />
<add key="SmtpFromEmail" value="your_verified_sender@example.com" />
```

---

## 13. Nếu vẫn chưa thấy mail OTP

Nếu log vẫn là:

```text
[Auth] Reset OTP sent user=... email=...
```

nhưng Gmail inbox không thấy, kiểm tra theo thứ tự:

1. Spam / Promotions / Updates.
2. Brevo Transactional logs:
   - Delivered
   - Deferred
   - Blocked
   - Soft bounce
   - Hard bounce
3. Sender trong `SmtpFromEmail` đã được Brevo xác thực/chấp nhận chưa.
4. Gmail có đang gom thread/categorize mail theo sender/subject không.
5. So sánh raw headers của mail `swaks` và mail app trong Brevo logs.

---

## 14. Kết luận

- Vấn đề timeout Gmail SMTP là do provider/VPS block outbound `465/587`.
- Brevo SMTP relay qua `2525` hoạt động.
- `swaks` đã xác nhận credential Brevo đúng.
- App ban đầu khác `swaks` ở `From`, nên đã tách `SmtpFromEmail`.
- Reset password xác định user theo email nhập ở form Forgot Password; OTP lưu kèm `UserId`, reset thành công update password cho `entry.UserId`.