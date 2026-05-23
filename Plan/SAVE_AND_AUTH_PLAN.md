# SAVE_AND_AUTH_PLAN

> Kế hoạch triển khai chi tiết 2 tính năng:
> 1. Nâng cấp lưu tài liệu thành 3 loại (manual / periodic / draft).
> 2. Quên mật khẩu — xác thực qua Gmail OTP.
>
> Tài liệu này dùng cho cả người đọc và AI khác triển khai code. Phân tích dựa trên file thực tế đã đọc (không phải giả định).
>
> **Trạng thái:** Đã phê duyệt phương án — chưa code. Khi bắt đầu implement, làm theo thứ tự Phase ở mục cuối.
>
> **Nguyên tắc bắt buộc:**
> - Không refactor lớn, không đổi kiến trúc.
> - Không đổi DB schema (tận dụng cột `label` + in-memory store).
> - Không phá backward compatibility.
> - Không hardcode secret, không log dữ liệu nhạy cảm.
> - Mọi phát sinh ngoài kế hoạch → ghi vào `feature.md` riêng để duyệt trước.

---

## TÍNH NĂNG 1: Nâng cấp lưu tài liệu — 3 loại bản lưu

### A. Hiện trạng project

**Client — `MarkTogether.Client/TypeRenderForm.cs`:**
- Chỉ có 1 cơ chế save duy nhất: `SocketClient.Instance.SaveDocument(_docId, content)`.
- 3 trigger gọi save:
  1. `btnSave_Click` (line 878) — manual save, tiêu đề form đổi `(đã lưu lúc HH:mm:ss)`.
  2. `AutosaveTimer_Tick` (line 495) — chu kỳ cố định 30s (`AutosaveIntervalMs = 30000` line 55), chỉ chạy khi `_hasUnsavedChanges == true`.
  3. `OnFormClosing` (line 1546) — save lần cuối khi có quyền edit.
- KHÔNG có debounce-after-typing autosave (debounce 280 ms chỉ áp dụng cho render preview, không liên quan).
- KHÔNG có UI bật/tắt autosave hay chọn interval.

**Server — `MarkTogether.Server/Network/ClientHandler.cs` → `HandleDocSave` (line 360–409):**
- Mọi `DOC_SAVE` đều thực hiện y hệt:
  1. RBAC `CanEdit`.
  2. `DocumentRepository.UpdateContent(docId, newContent)` ghi đè `documents.content`.
  3. `DocumentVersionRepository.SaveVersion(...)` luôn tạo version mới với `label = "Saved by {username}"`.
  4. `TrimVersions(docId, 50)` giữ tối đa 50 versions.
- → Không phân biệt manual / auto / draft. Autosave 30s đang spam version history.

**Bảng `document_versions` (`schema_init.sql` line 101–112):**
- Cột: `id`, `doc_id`, `content_snapshot`, `saved_by`, `saved_at`, `label VARCHAR(200)`.
- `label` là free-text, không có cột phân loại kind.

### B. Khoảng thiếu

1. Thiếu draft save debounce 2s sau khi dừng gõ.
2. Thiếu UI bật/tắt periodic autosave + chọn interval 1/5/30 phút (interval đang hardcode 30s).
3. Không phân loại được 3 loại version trong DB → cần convention prefix `label`.
4. Draft cần upsert (ghi đè bản trước), không tạo version mới mỗi lần → cần repo method mới.
5. VersionHistoryForm hiển thị 1 list chung, không phân biệt loại (optional cải thiện UX).

### C. File liên quan cần đọc/chỉnh

| File | Loại sửa |
|------|---------|
| `MarkTogether.Shared/Packet.cs` | Thêm field `kind` + `periodicIntervalMin` vào `Payload_DOC_SAVE_Request` |
| `MarkTogether.Client/Network/SocketClient.cs` | Thêm tham số `kind` vào `SaveDocument(...)` |
| `MarkTogether.Client/TypeRenderForm.cs` | Thêm `_draftTimer` (2s), đổi `_autosaveTimer` thành interval động, gọi save với `kind` |
| `MarkTogether.Client/TypeRenderForm.Designer.cs` | Thêm `chkPeriodicAutosave` + `cmbAutosaveInterval` trong toolbar |
| `MarkTogether.Server/Network/ClientHandler.cs` | Sửa `HandleDocSave` phân nhánh theo `kind` |
| `MarkTogether.Server/Database/Repositories/DocumentVersionRepository.cs` | Thêm `UpsertDraftVersion(docId, content, savedBy, label)` |
| `MarkTogether.Client/VersionHistoryForm.cs` (optional) | Hiển thị badge "thủ công/định kỳ/nháp" theo prefix label |

**KHÔNG đụng:** `User.cs`, `AuthService.cs`, `DocumentRepository.UpdateContent`, OT pipeline, share/comment/chat/image/AI.

### D. Kế hoạch chỉnh sửa tối thiểu

#### D.1. Phân loại 3 loại bằng prefix `label` (KHÔNG đổi schema)

Quy ước:
- Manual → `label = "manual:Saved by {username}"`
- Periodic → `label = "periodic:{intervalMin}m by {username}"`
- Draft → `label = "draft:by {username}"`

Query phân biệt: `WHERE label LIKE 'draft:%'`. Client parse prefix để render icon.

#### D.2. `Shared/Packet.cs` — mở rộng payload

```csharp
public class Payload_DOC_SAVE_Request {
    public string docID { get; set; }
    public string content { get; set; }
    public string kind { get; set; } = "manual";       // "manual" | "periodic" | "draft"
    public int periodicIntervalMin { get; set; } = 0;  // chỉ dùng khi kind="periodic"
}
```

Backward-compat: client/server cũ gửi packet không có `kind` → server treat như `"manual"`.

#### D.3. Server `ClientHandler.HandleDocSave` — branching theo `kind`

```csharp
string kind = (payload.kind ?? "manual").Trim().ToLowerInvariant();
if (kind != "draft" && kind != "periodic" && kind != "manual") kind = "manual";

if (!DocumentRepository.UpdateContent(docId, newContent)) { ReplyError(...); return; }

try {
    if (kind == "draft") {
        DocumentVersionRepository.UpsertDraftVersion(docId, newContent, currentUserId,
            $"draft:by {_username}");
        // KHÔNG TrimVersions cho draft (draft luôn đúng 1 row per user-doc)
    } else {
        string label = kind == "periodic"
            ? $"periodic:{payload.periodicIntervalMin}m by {_username}"
            : $"manual:Saved by {_username}";
        DocumentVersionRepository.SaveVersion(new DocumentVersion {
            DocId = docId, ContentSnapshot = newContent, SavedBy = currentUserId, Label = label
        });
        TrimVersions(docId, 50);
    }
} catch (Exception ex) { Console.WriteLine($"[Handler] Lưu version thất bại: {ex.Message}"); }
ReplyOk(packet, "Lưu tài liệu thành công");
```

#### D.4. `DocumentVersionRepository.UpsertDraftVersion` (method mới)

```csharp
public static void UpsertDraftVersion(string docId, string content, int savedBy, string label)
{
    using (IDbConnection db = DbConnectionFactory.CreateConnection()) {
        db.Open();
        int affected = db.Execute(
            @"UPDATE document_versions
              SET content_snapshot = @C, saved_at = NOW(), label = @L
              WHERE doc_id = @D AND saved_by = @U AND label LIKE 'draft:%'",
            new { C = content, L = label, D = docId, U = savedBy });
        if (affected > 0) return;
        db.Execute(
            @"INSERT INTO document_versions (doc_id, content_snapshot, saved_by, label)
              VALUES (@D, @C, @U, @L)",
            new { D = docId, C = content, U = savedBy, L = label });
    }
}
```

Filter `LIKE 'draft:%'` chỉ match draft của user đó → KHÔNG ghi đè manual/periodic.

#### D.5. Client `SocketClient.SaveDocument` — thêm tham số

```csharp
public void SaveDocument(string docId, string content, string kind = "manual", int periodicMin = 0)
    => Request(MessageType.DOC_SAVE, new Payload_DOC_SAVE_Request {
        docID = docId, content = content, kind = kind, periodicIntervalMin = periodicMin
    });
```

Callers cũ không truyền `kind` → default `"manual"` → giữ behavior.

#### D.6. Client `TypeRenderForm` — 3 timer riêng

**Fields mới:**
```csharp
private Timer _draftTimer;            // 2s debounce
private bool _periodicEnabled;
private int _periodicIntervalMs;      // 60_000 | 300_000 | 1_800_000
```

**Constructor:**
```csharp
_draftTimer = new Timer { Interval = 2000 };
_draftTimer.Tick += DraftTimer_Tick;
// _autosaveTimer.Start() KHÔNG gọi mặc định nữa — chỉ start khi user bật chk
```

**`txtRawMarkdown_TextChanged`** — thêm sau block hiện tại:
```csharp
if (!_suppressOpTracking && _hasUnsavedChanges && !string.IsNullOrWhiteSpace(_docId)
    && (_permission == "owner" || _permission == "editor")) {
    _draftTimer.Stop();
    _draftTimer.Start();
}
```

**`DraftTimer_Tick`:**
```csharp
private async void DraftTimer_Tick(object sender, EventArgs e) {
    _draftTimer.Stop();
    if (!_hasUnsavedChanges || string.IsNullOrWhiteSpace(_docId)) return;
    if (!(_permission == "owner" || _permission == "editor")) return;
    try {
        string content = txtRawMarkdown.Text ?? "";
        await Task.Run(() => SocketClient.Instance.SaveDocument(_docId, content, "draft"));
        // CỐ Ý KHÔNG reset _hasUnsavedChanges và _lastSavedContent — draft không phải save chính thức
    } catch { /* silent */ }
}
```

**`AutosaveTimer_Tick`** — đổi để gọi periodic:
```csharp
await Task.Run(() => SocketClient.Instance.SaveDocument(
    _docId, content, "periodic", _periodicIntervalMs / 60_000));
```

**UI handlers:**
```csharp
private void chkPeriodicAutosave_CheckedChanged(object sender, EventArgs e) {
    _periodicEnabled = chkPeriodicAutosave.Checked;
    cmbAutosaveInterval.Enabled = _periodicEnabled;
    ApplyPeriodicTimer();
}
private void cmbAutosaveInterval_SelectedIndexChanged(object sender, EventArgs e) {
    switch (cmbAutosaveInterval.SelectedIndex) {
        case 0: _periodicIntervalMs = 60_000; break;
        case 1: _periodicIntervalMs = 300_000; break;
        case 2: _periodicIntervalMs = 1_800_000; break;
    }
    ApplyPeriodicTimer();
}
private void ApplyPeriodicTimer() {
    _autosaveTimer.Stop();
    if (_periodicEnabled && _periodicIntervalMs > 0 && _trackRealtimeOps) {
        _autosaveTimer.Interval = _periodicIntervalMs;
        _autosaveTimer.Start();
    }
}
```

**Lưu setting bật/tắt periodic:** Phương án tối thiểu — chỉ giữ in-memory mỗi lần mở doc (default tắt). Nếu cần persist, tận dụng pattern `AISettingsStore` (file JSON ở `%AppData%\MarkTogether\autosave.json`) — không cần ngay phase 1.

#### D.7. `VersionHistoryForm` (optional, ít xâm phạm UI)

Trong `ReloadAsync`, parse `v.label` để hiển thị badge ở subitem hiện có (không thêm cột → không đổi Designer):
```csharp
string badge = "📝 Thủ công";
if (v.label?.StartsWith("periodic:") == true) badge = "⏱ Định kỳ";
else if (v.label?.StartsWith("draft:") == true) badge = "💾 Nháp";
```

### E. Flow sau khi sửa

**Draft (debounce 2s):**
1. User gõ → `TextChanged` → reset `_draftTimer`.
2. Dừng gõ 2s → `DraftTimer_Tick` → `SaveDocument(kind="draft")`.
3. Server `UpsertDraftVersion`: nếu đã có row `(doc, user, label LIKE 'draft:%')` → UPDATE; chưa có → INSERT.
4. → Mỗi (user, doc) chỉ có 1 draft. Không spam history.

**Periodic (1/5/30 phút):**
1. User bật `chkPeriodicAutosave` + chọn interval.
2. `_autosaveTimer.Interval` set, `.Start()`.
3. Mỗi tick, nếu `_hasUnsavedChanges` → `SaveDocument(kind="periodic", periodicMin=...)`.
4. Server tạo version mới `label = "periodic:5m by alice"`.

**Manual:**
1. User bấm `btnSave` → `SaveDocument(kind="manual")`.
2. Server tạo version mới `label = "manual:Saved by alice"`.

**Tính độc lập:**
- Draft chỉ ghi đè draft cùng user (WHERE filter).
- Periodic & Manual luôn INSERT mới → không ghi đè nhau.
- `TrimVersions(50)` chỉ áp dụng cho manual/periodic.

### F. Database/schema

**KHÔNG đổi schema.** Tận dụng cột `label VARCHAR(200)` (đủ chỗ cho prefix + username dài) + `WHERE label LIKE 'draft:%'` để upsert đúng row draft.

**Lý do:** tránh migration, backward-compat hoàn toàn — version cũ có `label="Saved by alice"` vẫn hiển thị bình thường (không match prefix mới nên fallback "Thủ công").

### G. Việc bạn cần tự làm

1. Không cần action với database.
2. Rebuild solution (Shared + Server + Client) vì `Packet.cs` đổi shape.
3. (Tuỳ chọn) cập nhật `DOCUMENTATION.md` mục **7.7. Lưu tài liệu** để mô tả 3 loại save.
4. Chạy test checklist H.1.

### H.1. Test checklist — Save

| # | Test | Kỳ vọng |
|---|------|---------|
| 1 | Gõ vài chữ, dừng 2s | Server log nhận `kind=draft`. `document_versions` có +1 row `label='draft:by ...'`. |
| 2 | Tiếp tục gõ + dừng 2s lần 2 | Row draft cũ được UPDATE, không +1 row mới. |
| 3 | Gõ liên tục 10s không nghỉ | `_draftTimer` reset liên tục, KHÔNG fire save. |
| 4 | Bật periodic 1 phút, gõ + đợi 1 phút | +1 version `label='periodic:1m by ...'`. |
| 5 | Đợi tiếp 1 phút nữa (chỉnh nội dung) | +1 version periodic riêng biệt (KHÔNG ghi đè bản trước). |
| 6 | Tắt `chkPeriodicAutosave` | `_autosaveTimer` stop. Đợi quá interval không có save mới. |
| 7 | Bấm Save thủ công 3 lần (sửa giữa các lần) | 3 version `label='manual:Saved by ...'` riêng biệt. |
| 8 | Manual + Periodic + Draft xen kẽ trong cùng phiên | Draft luôn upsert 1 row; manual/periodic luôn +1 row; tổng version trong DB tăng đúng. |
| 9 | Login bằng quyền `viewer`/`commenter`, thử cả 3 kind | Server reply ERROR `"Bạn không có quyền lưu tài liệu này."` cho cả 3. |
| 10 | Kill client lúc đang gõ → restart → mở lại doc | Draft đã lưu trước đó còn trong `document_versions`, có thể restore. |
| 11 | Client mới nói chuyện với server cũ (chưa deploy đồng bộ) | Server cũ ignore field `kind` → tất cả save chạy như `manual` (graceful degrade). |
| 12 | VersionHistoryForm | Hiển thị đúng badge cho từng loại. |

---

## TÍNH NĂNG 2: Forgot Password qua Gmail

### A. Hiện trạng project

**Auth — `MarkTogether.Server/Services/AuthService.cs`:**
- `Register(username, email, password)` — email optional, check trùng nếu có (line 39–44).
- `Login(username, password)` — BCrypt verify, rate-limit 5 lần/15 phút qua `_loginFails` ConcurrentDictionary (line 15).
- Pattern `_loginFails` tham khảo được cho OTP store.

**User schema:**
- `users.email VARCHAR(100) UNIQUE` đã có (`schema_init.sql` line 30).
- `UserRepository.UpdatePassword(id, hash)` đã có sẵn (line 101), dùng được nguyên.
- `User.Email` POCO line 13.

**KHÔNG có sẵn:**
- Endpoint forgot/reset password.
- SMTP code / `System.Net.Mail` usage.
- Bảng OTP/reset token.
- App.config SMTP keys.
- UI "Quên mật khẩu" trong `LoginForm`.

**Có thể tận dụng:**
- `SecureTokenGenerator` (CSPRNG).
- `App.config appSettings` pattern (`GeminiApiKey`, `TlsCertPath`).
- `System.Net.Mail.SmtpClient` — có sẵn trong .NET Framework 4.7.2, **không cần NuGet**.

### B. Khoảng thiếu

1. Không có flow forgot password ở mọi tầng (UI/protocol/service/storage).
2. Không có SMTP service — phải viết wrapper mới.
3. Không có nơi lưu OTP — chọn in-memory để tránh migration.
4. App.config thiếu SMTP keys.
5. LoginForm thiếu link "Quên mật khẩu".

### C. File liên quan cần đọc/chỉnh

| File | Loại sửa |
|------|---------|
| `MarkTogether.Shared/Packet.cs` | Thêm 2 `MessageType` + 4 `Payload_*` classes |
| `MarkTogether.Server/Services/EmailService.cs` (mới) | Wrapper `System.Net.Mail.SmtpClient` |
| `MarkTogether.Server/Services/AuthService.cs` | Thêm `RequestPasswordResetAsync` + `ResetPassword` + `_resetOtps` dict |
| `MarkTogether.Server/Network/ClientHandler.cs` | 2 case mới trong switch + 2 handler |
| `MarkTogether.Server/App.config` | Thêm 6 key SMTP trong `appSettings` |
| `MarkTogether.Client/Network/SocketClient.cs` | 2 method mới `RequestPasswordReset` + `ResetPassword` |
| `MarkTogether.Client/LoginForm.cs / .Designer.cs` | Thêm `lnkForgotPassword` |
| `MarkTogether.Client/ForgotPasswordForm.cs / .Designer.cs` (mới) | Modal 2 bước: email → OTP + new password |

**KHÔNG đụng:** `UserRepository.UpdatePassword` (dùng nguyên), `User` model, DB schema, OT/Doc/Share/AI.

### D. Kế hoạch chỉnh sửa tối thiểu

#### D.1. `App.config` — SMTP keys

```xml
<appSettings>
  <!-- existing keys giữ nguyên -->
  <add key="SmtpHost" value="smtp.gmail.com" />
  <add key="SmtpPort" value="587" />
  <add key="SmtpUser" value="" />
  <add key="SmtpPassword" value="" />
  <add key="SmtpFromName" value="MarkTogether" />
  <add key="SmtpEnableSsl" value="true" />
</appSettings>
```

Production: override bằng env var (giống pattern `MARKTOGETHER_DB_CONNECTION`) — phase 1 chỉ cần `App.config`.

#### D.2. `EmailService.cs` (mới)

```csharp
using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MarkTogether.Server.Services {
    public static class EmailService {
        public static async Task SendOtpAsync(string toEmail, string username, string otp) {
            string host = ConfigurationManager.AppSettings["SmtpHost"];
            int port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            string user = ConfigurationManager.AppSettings["SmtpUser"];
            string pass = ConfigurationManager.AppSettings["SmtpPassword"];
            string fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "MarkTogether";
            bool ssl = (ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true")
                .Equals("true", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("SMTP chưa được cấu hình trên server.");

            using (var client = new SmtpClient(host, port)) {
                client.EnableSsl = ssl;
                client.Credentials = new NetworkCredential(user, pass);
                var msg = new MailMessage(new MailAddress(user, fromName), new MailAddress(toEmail)) {
                    Subject = "MarkTogether — Mã xác thực đặt lại mật khẩu",
                    Body = $"Xin chào {username},\n\nMã xác thực: {otp}\n" +
                           "Mã có hiệu lực 10 phút.\nNếu bạn không yêu cầu, hãy bỏ qua email này.\n\n— MarkTogether",
                    IsBodyHtml = false
                };
                await client.SendMailAsync(msg).ConfigureAwait(false);
            }
        }
    }
}
```

#### D.3. `Shared/Packet.cs` — 2 message type + 4 payload

```csharp
public enum MessageType {
    // ... existing ...
    AUTH_FORGOT_PASSWORD,
    AUTH_RESET_PASSWORD,
}

public class Payload_AUTH_FORGOT_PASSWORD_Request  { public string Email { get; set; } }
public class Payload_AUTH_FORGOT_PASSWORD_Response { public bool Success { get; set; } public string Message { get; set; } }

public class Payload_AUTH_RESET_PASSWORD_Request {
    public string Email { get; set; }
    public string Otp { get; set; }
    public string NewPassword { get; set; }
}
public class Payload_AUTH_RESET_PASSWORD_Response {
    public bool Success { get; set; }
    public string Message { get; set; }
}
```

#### D.4. `AuthService` — OTP store + 2 method

```csharp
private static readonly ConcurrentDictionary<string, OtpEntry> _resetOtps = new ConcurrentDictionary<string, OtpEntry>();
private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
private static readonly TimeSpan OtpResendCooldown = TimeSpan.FromSeconds(60);

private struct OtpEntry {
    public string OtpHash;
    public DateTime ExpiresAtUtc;
    public DateTime CreatedAtUtc;
    public int UserId;
}

public static async Task<Payload_AUTH_FORGOT_PASSWORD_Response> RequestPasswordResetAsync(string email) {
    if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        return new Payload_AUTH_FORGOT_PASSWORD_Response { Success = false, Message = "Email không hợp lệ." };

    string key = email.Trim().ToLowerInvariant();
    var user = UserRepository.GetByEmail(key);
    var generic = new Payload_AUTH_FORGOT_PASSWORD_Response { Success = true,
        Message = "Nếu email tồn tại, mã OTP đã được gửi." };

    if (user == null) return generic;  // anti-enumeration

    if (_resetOtps.TryGetValue(key, out var prev)
        && (DateTime.UtcNow - prev.CreatedAtUtc) < OtpResendCooldown) return generic;

    string otp = GenerateOtp6();
    _resetOtps[key] = new OtpEntry {
        OtpHash = Sha256(otp),
        ExpiresAtUtc = DateTime.UtcNow + OtpLifetime,
        CreatedAtUtc = DateTime.UtcNow,
        UserId = user.Id
    };

    try {
        await EmailService.SendOtpAsync(user.Email, user.Username, otp);
        Console.WriteLine($"[Auth] Reset OTP sent user={user.Id} email={Mask(user.Email)}");
    } catch (Exception ex) {
        Console.WriteLine($"[Auth] SMTP send failed user={user.Id}: {ex.Message}");
    }
    return generic;
}

public static Payload_AUTH_RESET_PASSWORD_Response ResetPassword(string email, string otp, string newPassword) {
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPassword))
        return new Payload_AUTH_RESET_PASSWORD_Response { Success = false, Message = "Thiếu thông tin." };
    if (newPassword.Length < 6)
        return new Payload_AUTH_RESET_PASSWORD_Response { Success = false, Message = "Mật khẩu phải có ít nhất 6 ký tự." };

    string key = email.Trim().ToLowerInvariant();
    if (!_resetOtps.TryGetValue(key, out var entry))
        return new Payload_AUTH_RESET_PASSWORD_Response { Success = false, Message = "OTP không hợp lệ hoặc đã hết hạn." };

    if (DateTime.UtcNow > entry.ExpiresAtUtc) {
        _resetOtps.TryRemove(key, out _);
        return new Payload_AUTH_RESET_PASSWORD_Response { Success = false, Message = "OTP đã hết hạn." };
    }

    if (Sha256(otp.Trim()) != entry.OtpHash)
        return new Payload_AUTH_RESET_PASSWORD_Response { Success = false, Message = "OTP không đúng." };

    string hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
    if (!UserRepository.UpdatePassword(entry.UserId, hash))
        return new Payload_AUTH_RESET_PASSWORD_Response { Success = false, Message = "Không cập nhật được mật khẩu." };

    _resetOtps.TryRemove(key, out _);       // one-shot
    _loginFails.TryRemove(key, out _);      // clear khoá login fail nếu có
    Console.WriteLine($"[Auth] Password reset success user={entry.UserId}");
    return new Payload_AUTH_RESET_PASSWORD_Response { Success = true, Message = "Đặt lại mật khẩu thành công." };
}

private static string GenerateOtp6() {
    var b = new byte[4];
    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create()) rng.GetBytes(b);
    return ((BitConverter.ToInt32(b, 0) & 0x7FFFFFFF) % 1000000).ToString("D6");
}
private static string Sha256(string s) {
    using (var sha = System.Security.Cryptography.SHA256.Create()) {
        var b = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
        return BitConverter.ToString(b).Replace("-", "").ToLowerInvariant();
    }
}
private static string Mask(string email) {
    if (string.IsNullOrEmpty(email) || !email.Contains("@")) return "***";
    var p = email.Split('@');
    return p[0].Substring(0, Math.Min(2, p[0].Length)) + "***@" + p[1];
}
```

#### D.5. `ClientHandler` — dispatch 2 case mới

Trong `ProcessAsync` switch (sau case `AUTH_LOGOUT`):
```csharp
case MessageType.AUTH_FORGOT_PASSWORD: HandleForgotPassword(packet); break;
case MessageType.AUTH_RESET_PASSWORD:  HandleResetPassword(packet); break;
```

Handlers (cùng vùng AUTH):
```csharp
private void HandleForgotPassword(Packet packet) {
    var p = packet.GetPayload<Payload_AUTH_FORGOT_PASSWORD_Request>();
    Packet origin = packet;
    Task.Run(async () => {  // SMTP blocking → tránh chặn loop chính
        var resp = await AuthService.RequestPasswordResetAsync(p?.Email);
        Reply(origin, MessageType.AUTH_FORGOT_PASSWORD, resp);
    });
}

private void HandleResetPassword(Packet packet) {
    var p = packet.GetPayload<Payload_AUTH_RESET_PASSWORD_Request>();
    var resp = AuthService.ResetPassword(p?.Email, p?.Otp, p?.NewPassword);
    Reply(packet, MessageType.AUTH_RESET_PASSWORD, resp);
}
```

Lưu ý: 2 handler này KHÔNG yêu cầu login — đúng intent (user đã quên mật khẩu).

#### D.6. Client `SocketClient` — 2 method

```csharp
public Payload_AUTH_FORGOT_PASSWORD_Response RequestPasswordReset(string email)
    => Request<Payload_AUTH_FORGOT_PASSWORD_Response>(
        MessageType.AUTH_FORGOT_PASSWORD,
        new Payload_AUTH_FORGOT_PASSWORD_Request { Email = email });

public Payload_AUTH_RESET_PASSWORD_Response ResetPassword(string email, string otp, string newPassword)
    => Request<Payload_AUTH_RESET_PASSWORD_Response>(
        MessageType.AUTH_RESET_PASSWORD,
        new Payload_AUTH_RESET_PASSWORD_Request { Email = email, Otp = otp, NewPassword = newPassword });
```

#### D.7. `LoginForm` — thêm LinkLabel

Trong `LoginForm.Designer.cs` thêm `lnkForgotPassword` cạnh `lnkRegister`. Handler:
```csharp
private void lnkForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
    try { SocketClient.Instance.Connect("159.203.184.87", 5000); } catch { /* retry trong dlg */ }
    using (var dlg = new ForgotPasswordForm()) dlg.ShowDialog(this);
}
```

#### D.8. `ForgotPasswordForm` — UI 2 bước

**Bước 1:** TextBox email + button "Gửi mã" → gọi `RequestPasswordReset` → đổi panel sang Bước 2.
**Bước 2:** Label "Mã đã gửi tới <email_masked>" + TextBox OTP (mask `\d{6}`) + TextBox new password + TextBox confirm + button "Đặt lại" → gọi `ResetPassword`. Thành công → MessageBox → close.

Style theo `UiFactory` để consistency với LoginForm/RegisterForm.

### E. Flow sau khi sửa

1. LoginForm → click "Quên mật khẩu?" → mở `ForgotPasswordForm`.
2. User nhập email → bấm "Gửi mã".
3. Client → `AUTH_FORGOT_PASSWORD { Email }`.
4. Server:
   - Lookup `GetByEmail`. Nếu có → sinh OTP 6 số, hash SHA-256 lưu vào `_resetOtps[email]` (TTL 10 phút).
   - Gửi mail SMTP qua Gmail App Password.
5. Server reply generic `"Nếu email tồn tại, mã OTP đã được gửi."` (anti-enumeration).
6. UI chuyển Bước 2.
7. User nhập OTP + mật khẩu mới → bấm "Đặt lại".
8. Client → `AUTH_RESET_PASSWORD { Email, Otp, NewPassword }`.
9. Server: tra `_resetOtps`, kiểm expiry, so SHA-256, BCrypt hash password mới, `UserRepository.UpdatePassword`, remove OTP, clear `_loginFails`.
10. Reply `{ Success=true }` → MessageBox → user quay về LoginForm → login bằng pwd mới.

### F. Database/schema

**KHÔNG đổi schema.**

- OTP lưu in-memory (`ConcurrentDictionary` trong `AuthService` — pattern y hệt `_loginFails`).
- `users.email` đã có UNIQUE.
- `UserRepository.UpdatePassword` đã sẵn.

**Trade-off:**
- ✅ Không migration, không deploy DB.
- ⚠️ Server restart trong cửa sổ 10 phút OTP → user phải xin lại OTP (chấp nhận được cho đồ án).
- ⚠️ Multi-instance server không share state (production cần Redis hoặc bảng `password_reset_tokens` — ngoài phạm vi).

Nếu sau này cần persist: bảng đề xuất `password_reset_tokens (id, user_id, otp_hash, expires_at, used_at)` — không phải bây giờ.

### G. Việc bạn cần tự làm

1. **Tạo Gmail App Password:**
   - Bật 2FA tại https://myaccount.google.com/security.
   - Vào "App passwords" → tạo password mới đặt tên "MarkTogether" → copy 16 ký tự.
2. **Cập nhật `MarkTogether.Server/App.config`:**
   ```xml
   <add key="SmtpUser" value="your_system_email@gmail.com" />
   <add key="SmtpPassword" value="abcd efgh ijkl mnop" />  <!-- App Password, KHÔNG phải mật khẩu Gmail thật -->
   ```
3. **KHÔNG commit `App.config` chứa App Password lên git.** Production: dùng env var hoặc `/etc/marktogether/env`.
4. **Đảm bảo user trong DB có email.** User đăng ký không nhập email → không thể reset (đúng behavior). Có thể thêm tooltip "Khuyến nghị nhập email để khôi phục mật khẩu" trong RegisterForm.
5. **Firewall outbound:** server cần access `smtp.gmail.com:587` (TLS). Kiểm tra trên VPS.
6. Rebuild solution → test theo H.2.

### H.2. Test checklist — Forgot Password

| # | Test | Kỳ vọng |
|---|------|---------|
| 1 | LoginForm hiển thị link "Quên mật khẩu?" | Click → ForgotPasswordForm mở ra. |
| 2 | Bước 1: nhập email tồn tại → "Gửi mã" | Nhận email Gmail trong vài giây. UI chuyển Bước 2. Server log `Reset OTP sent user=X email=al***@...`. |
| 3 | Bước 1: nhập email KHÔNG tồn tại | UI vẫn chuyển Bước 2 với message generic. KHÔNG có mail gửi. |
| 4 | Bước 1: nhập email sai format | "Email không hợp lệ." |
| 5 | Bấm "Gửi mã" 5 lần trong 30s | Chỉ mail đầu tiên thực sự gửi (cooldown 60s) — các lần sau im lặng return generic. |
| 6 | Bước 2: OTP đúng + mật khẩu mới ≥ 6 ký tự | "Đặt lại mật khẩu thành công." DB: `password_hash` đã đổi. Login pwd mới → OK. |
| 7 | Bước 2: OTP sai | "OTP không đúng." `password_hash` không đổi. |
| 8 | Đợi 11 phút rồi nhập OTP đúng | "OTP đã hết hạn." OTP bị remove. |
| 9 | Reset thành công → dùng cùng OTP lần 2 | "OTP không hợp lệ hoặc đã hết hạn." (one-shot). |
| 10 | Mật khẩu mới < 6 ký tự | "Mật khẩu phải có ít nhất 6 ký tự." |
| 11 | User đang bị lock vì 5 lần fail → reset xong → login | `_loginFails[email]` đã clear → login ngay được. |
| 12 | Grep server console output | Không xuất hiện OTP plaintext (chỉ có hash trong memory). |
| 13 | SMTP cấu hình sai (App Password sai) | Server log `SMTP send failed: ...` nhưng client vẫn nhận generic message → không leak. |
| 14 | User đăng ký không nhập email → quên pwd | `GetByEmail` null → generic message → không reset được (đúng). |

---

## RÀNG BUỘC & XỬ LÝ NGOÀI KẾ HOẠCH

| Ràng buộc | Tuân thủ |
|----------|----------|
| Không tự ý refactor lớn | ✅ Mọi thay đổi incremental |
| Không đổi kiến trúc | ✅ Vẫn TCP/JSON/MessageType |
| Không đổi schema | ✅ Tận dụng `label` prefix + in-memory OTP |
| Không phá backward-compat | ✅ Field `kind` default `"manual"` |
| Không hardcode secret | ✅ SMTP qua App.config (khuyến nghị env var prod) |
| Không log thông tin nhạy cảm | ✅ OTP hash SHA-256 trước khi lưu, log email masked |
| Không sửa module không liên quan | ✅ Chỉ chạm Save/Version + Auth |

Nếu trong implementation phát hiện cần đổi ngoài kế hoạch (vd `label VARCHAR(200)` không đủ, Gmail block port 587 trên VPS hosting, hoặc OTP cần persist qua restart), AI thực thi PHẢI tạo riêng file `feature.md` để bạn duyệt trước.

---

## CẬP NHẬT `DOCUMENTATION.md` ĐỀ XUẤT (sau khi implement)

- Mục **7.7. Lưu tài liệu** — bổ sung 3 loại save + bảng kind/label/behavior.
- Mục **7.2 / 7.3** — thêm flow quên mật khẩu.
- Mục **11.2** — note về OTP bcrypt + SHA-256 hashed OTP.
- Mục **5.1.3** — thêm SMTP cấu hình tương tự AI.

Không ghi chi tiết implementation, chỉ tổng quan. Chi tiết để trong file này.

---

## PHASE TRIỂN KHAI ĐỀ XUẤT

Khi bạn yêu cầu code, làm theo thứ tự sau (mỗi phase tự build + smoke test trước khi sang phase tiếp):

### Phase 1: Save — Backend
1. `Shared/Packet.cs` (mở rộng `Payload_DOC_SAVE_Request`).
2. `DocumentVersionRepository.UpsertDraftVersion`.
3. `ClientHandler.HandleDocSave` branching theo `kind`.
4. Build + test bằng client cũ → vẫn chạy `kind=manual` mặc định (smoke test backward-compat).

### Phase 2: Save — Frontend
5. `SocketClient.SaveDocument(... kind, periodicMin)`.
6. `TypeRenderForm` — `_draftTimer` + UI handlers + `ApplyPeriodicTimer`.
7. `TypeRenderForm.Designer.cs` — `chkPeriodicAutosave` + `cmbAutosaveInterval`.
8. (Optional) `VersionHistoryForm` — badge prefix.
9. Test toàn bộ H.1.

### Phase 3: Forgot Password — Backend
10. `App.config` SMTP keys.
11. `Services/EmailService.cs` (mới).
12. `Shared/Packet.cs` (2 MessageType + 4 payload).
13. `AuthService` — OTP store + 2 method.
14. `ClientHandler` — 2 case dispatch + 2 handler.
15. Build + test bằng raw packet (Wireshark/test harness) trước khi build UI.

### Phase 4: Forgot Password — Frontend
16. `SocketClient` — 2 method mới.
17. `ForgotPasswordForm.cs / .Designer.cs` (mới).
18. `LoginForm` — `lnkForgotPassword`.
19. Test toàn bộ H.2.

### Phase 5: Document update
20. Cập nhật `DOCUMENTATION.md` theo các mục đề xuất phía trên.
21. Commit theo từng phase, không bundle.

---

**Tài liệu này được tạo: 2026-05-22.** Phải đồng bộ với code thực tế — code là nguồn sự thật, nếu lệch hãy cập nhật file này.
