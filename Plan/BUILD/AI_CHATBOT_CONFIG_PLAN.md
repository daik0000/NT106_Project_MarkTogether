# Kế hoạch chi tiết — Chatbot AI: cấu hình model + tự nhập API key

> **Mục tiêu:** Thay vì hard-code `GeminiApiKey` trong `MarkTogether.Server/App.config` và cố định model `gemini-1.5-flash-latest`, cho phép **từng người dùng**:
> 1. Chọn model AI (từ danh sách model Gemini hỗ trợ).
> 2. Tự nhập API key của riêng mình; key **không** lưu trên server và không hard-code.
>
> Tài liệu này được viết để một AI / dev khác đọc và triển khai end-to-end mà không cần đọc thêm DOCUMENTATION.md (mọi tham chiếu file:line đã ghi rõ).

---

## 1. Bối cảnh hiện tại (baseline — đừng đụng vào chỗ khác)

| File | Vai trò hiện tại | Trạng thái |
|------|------------------|------------|
| `MarkTogether.Server/Services/AISuggestionService.cs` (toàn bộ, ~93 dòng) | Static class, đọc `ConfigurationManager.AppSettings["GeminiApiKey"]`, endpoint cố định `gemini-1.5-flash-latest:generateContent` | **PHẢI sửa**: nhận thêm `model` + `apiKey` qua tham số |
| `MarkTogether.Server/Network/ClientHandler.cs` — `HandleAiRequest` (line 1622–1672) | Dispatcher `AI_REQUEST`, throttle 3 s/handler, max 20 000 ký tự | **PHẢI sửa**: lấy `model` + `apiKey` từ payload, forward |
| `MarkTogether.Shared/Packet.cs` — `Payload_AI_Request` (line 646–652), `Payload_AI_Response` (line 654–659) | Schema giao thức AI hiện chỉ có `docID, mode, userPrompt, contextText` | **PHẢI mở rộng**: thêm `apiKey`, `model`, `provider` |
| `MarkTogether.Client/Network/SocketClient.cs` — `AskAI` (line 543–556) | Wrapper gọi `MessageType.AI_REQUEST` | **PHẢI sửa**: chấp nhận thêm `apiKey`, `model`, `provider` |
| `MarkTogether.Client/TypeRenderForm.Designer.cs` — tab AI (line 326–368) | UI tab "Trợ lý AI": `cmbAiMode`, `txtAiPrompt`, `txtAiHistory`, `btnAiSend` | **PHẢI thêm**: nút `btnAiSettings` mở modal cấu hình |
| `MarkTogether.Client/TypeRenderForm.cs` — `btnAiSend_Click` (line 1278–~1315) | Gọi `SocketClient.Instance.AskAI(mode, prompt, ctx, _docId)` | **PHẢI sửa**: load settings từ `AISettingsStore`, gắn `apiKey/model` vào request |
| `MarkTogether.Server/App.config` (line 8–11, `GeminiApiKey`) | Hard-code key | **GIỮ** nhưng đổi giá trị thành `""` và comment rõ "fallback dev/demo, production user tự cấu hình" |

**Không** đụng đến: `OT/*`, `Database/*`, `SocketServer`, `SessionManager`, `DocumentPermissionService`, các handler khác trong `ClientHandler.cs`.

---

## 2. Quyết định kiến trúc

### 2.1. API key lưu ở đâu? → **Client-side, mã hoá DPAPI**

- Mỗi user lưu cấu hình tại `%AppData%\MarkTogether\ai_settings.json` (Windows). File này bind theo profile user (DPAPI `CurrentUser` scope) — copy file sang máy khác không decrypt được.
- Client đính kèm `apiKey` + `model` vào **từng** `Payload_AI_Request`.
- **Lý do chọn client-side** (so với lưu DB server `user_ai_settings`):
  - Không cần migration DB, không cần encryption-at-rest server-side, không cần endpoint quản lý key.
  - Server không biết key của ai → giảm bề mặt rủi ro nếu DB bị lộ.
  - Khớp với phạm vi đồ án; tránh phình scope.
- **Trade-off đã chấp nhận:** key đi qua TCP plaintext (cùng rủi ro với token hiện tại — xem `DOCUMENTATION.md` §11.6 / §16.3). Mitigation: khuyến cáo TLS qua stunnel khi deploy thật.

### 2.2. Provider nào? → **Phase 1: chỉ Gemini, nhiều model**

Trường `provider` được giữ chỗ trong payload để mở rộng (`"gemini" | "openai" | "anthropic"`), nhưng **server chỉ implement `"gemini"`** ở phase này. Provider khác → reply `"Provider chưa được hỗ trợ"`.

**Danh sách model whitelist** (server `AISuggestionService` và client `AISettingsStore.SupportedModels` phải đồng bộ):

```
gemini-1.5-flash-latest      ← mặc định
gemini-1.5-pro-latest
gemini-2.0-flash-exp
gemini-2.5-flash
gemini-2.5-pro
```

Endpoint động: `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}`.

### 2.3. Backward compatibility

Server **không** reject request thiếu field mới — fallback theo thứ tự:
1. `provider` rỗng → `"gemini"`.
2. `model` rỗng → `"gemini-1.5-flash-latest"`.
3. `apiKey` rỗng → đọc `ConfigurationManager.AppSettings["GeminiApiKey"]` (giữ behavior cũ cho dev local). Nếu cả 2 đều rỗng → reply `success=false, message="Chưa cấu hình API key. Mở 'Cài đặt AI' để nhập."`.

---

## 3. Thay đổi chi tiết theo từng file

### 3.1. `MarkTogether.Shared/Packet.cs` — mở rộng payload

Tại block `Payload_AI_Request` (line 646–652), thêm 3 field:

```csharp
public class Payload_AI_Request
{
    public string docID { get; set; }
    public string mode { get; set; }        // "chat" | "summarize" | "continue" | "translate"
    public string userPrompt { get; set; }
    public string contextText { get; set; }

    // NEW — cấu hình động phía client
    public string provider { get; set; }    // "gemini" (mặc định, reserved cho future)
    public string model { get; set; }       // ví dụ "gemini-1.5-flash-latest"
    public string apiKey { get; set; }      // có thể null/rỗng → server fallback App.config
}
```

`Payload_AI_Response` giữ nguyên, **không** đụng. Newtonsoft sẽ tự bỏ qua field thiếu trên client/server cũ (forward compat).

### 3.2. `MarkTogether.Server/Services/AISuggestionService.cs` — viết lại

Thay đổi:

1. Đổi chữ ký `AskAsync`:
   ```csharp
   public static async Task<string> AskAsync(
       string provider, string model, string apiKey,
       string mode, string userPrompt, string contextText)
   ```

2. Bỏ field `Endpoint` cố định. Build URL động:
   ```csharp
   private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
   // ...
   string url = $"{BaseUrl}{model}:generateContent?key={apiKey}";
   ```

3. Thêm whitelist model (HashSet readonly) — đồng bộ với danh sách mục §2.2. Trả `InvalidOperationException("Model không được hỗ trợ: " + model)` nếu không khớp.

4. Logic fallback (đặt đầu hàm):
   ```csharp
   provider = string.IsNullOrWhiteSpace(provider) ? "gemini" : provider.Trim().ToLowerInvariant();
   if (provider != "gemini")
       throw new InvalidOperationException("Provider chưa được hỗ trợ: " + provider);

   if (string.IsNullOrWhiteSpace(model))
       model = "gemini-1.5-flash-latest";
   if (!SupportedModels.Contains(model))
       throw new InvalidOperationException("Model không được hỗ trợ: " + model);

   if (string.IsNullOrWhiteSpace(apiKey))
       apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
   if (string.IsNullOrWhiteSpace(apiKey))
       throw new InvalidOperationException(
           "Chưa cấu hình API key. Mở 'Cài đặt AI' để nhập.");
   ```

5. Sửa parse lỗi HTTP — phân loại để báo user thân thiện hơn:
   ```csharp
   if (!res.IsSuccessStatusCode)
   {
       string msg = (int)res.StatusCode == 400 ? "API key không hợp lệ hoặc model sai."
                  : (int)res.StatusCode == 429 ? "Đã vượt quota của API key này."
                  : $"Gemini lỗi {(int)res.StatusCode}: {Truncate(body, 200)}";
       throw new InvalidOperationException(msg);
   }
   ```

6. Giữ nguyên `BuildPrompt(...)` (line 73–92). Không sửa.

### 3.3. `MarkTogether.Server/Network/ClientHandler.cs` — `HandleAiRequest`

Vị trí: line 1622–1672.

Sửa 2 chỗ:

1. Trước khi `Task.Run`, đọc thêm `p.provider`, `p.model`, `p.apiKey`. Log **hash 8 ký tự đầu** của `apiKey`, không log raw:
   ```csharp
   string keyHash = string.IsNullOrEmpty(p.apiKey) ? "(server-fallback)"
                  : Sha256Short(p.apiKey); // helper: trả 8 hex đầu của SHA-256
   Console.WriteLine($"[AI] user={currentUserId} provider={p.provider ?? "gemini"} " +
                     $"model={p.model ?? "default"} keyHash={keyHash} mode={p.mode}");
   ```
   `Sha256Short` đặt private static helper trong cùng class. **Không** dùng `Logger` chung nếu chưa có — `Console.WriteLine` đủ cho server console.

2. Đổi cuộc gọi `AskAsync` (line ~1659–1661):
   ```csharp
   string text = await AISuggestionService
       .AskAsync(p.provider, p.model, p.apiKey, p.mode, p.userPrompt, p.contextText)
       .ConfigureAwait(false);
   ```

Giữ nguyên throttle 3 s, max 20 000 ký tự, và toàn bộ control flow `Task.Run`.

### 3.4. `MarkTogether.Server/App.config`

Sửa line 8–11 → đổi giá trị `GeminiApiKey` thành rỗng, thêm comment:

```xml
<appSettings>
  <!-- Fallback API key cho dev/demo (không bắt buộc).
       Production: mỗi user tự nhập trong 'Cài đặt AI' phía client.
       Server KHÔNG hard-code key cho người dùng cuối. -->
  <add key="GeminiApiKey" value="" />
</appSettings>
```

Nếu repo đã commit key thật → cần **rotate key** (revoke ở Google AI Studio) sau khi merge.

### 3.5. `MarkTogether.Client/Services/AISettingsStore.cs` — **TẠO MỚI**

Tạo thư mục `MarkTogether.Client/Services/` nếu chưa có. Thêm file `.csproj` reference qua MSBuild item `<Compile Include="Services\AISettingsStore.cs" />`.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace MarkTogether.Client.Services
{
    public class AISettings
    {
        public string Provider { get; set; } = "gemini";
        public string Model    { get; set; } = "gemini-1.5-flash-latest";
        public string ApiKey   { get; set; } = "";
    }

    public static class AISettingsStore
    {
        public static readonly IReadOnlyList<string> SupportedModels = new[]
        {
            "gemini-1.5-flash-latest",
            "gemini-1.5-pro-latest",
            "gemini-2.0-flash-exp",
            "gemini-2.5-flash",
            "gemini-2.5-pro",
        };

        private static string SettingsPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MarkTogether");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "ai_settings.json");
            }
        }

        public static AISettings Load()
        {
            if (!File.Exists(SettingsPath)) return new AISettings();
            try
            {
                string json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                var dto = JsonConvert.DeserializeObject<PersistedDto>(json) ?? new PersistedDto();
                return new AISettings
                {
                    Provider = string.IsNullOrEmpty(dto.Provider) ? "gemini" : dto.Provider,
                    Model    = string.IsNullOrEmpty(dto.Model) ? "gemini-1.5-flash-latest" : dto.Model,
                    ApiKey   = Decrypt(dto.ApiKeyEncrypted)
                };
            }
            catch { return new AISettings(); }
        }

        public static void Save(AISettings s)
        {
            var dto = new PersistedDto
            {
                Provider = s.Provider,
                Model = s.Model,
                ApiKeyEncrypted = Encrypt(s.ApiKey ?? "")
            };
            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(dto, Formatting.Indented), Encoding.UTF8);
        }

        private static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            byte[] data = Encoding.UTF8.GetBytes(plain);
            byte[] enc  = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(enc);
        }

        private static string Decrypt(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return "";
            try
            {
                byte[] enc  = Convert.FromBase64String(b64);
                byte[] data = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch { return ""; }
        }

        private class PersistedDto
        {
            public string Provider { get; set; }
            public string Model { get; set; }
            public string ApiKeyEncrypted { get; set; }
        }
    }
}
```

**Yêu cầu:** project `Client.csproj` đã reference `System.Security.dll` (chứa `ProtectedData`). Nếu chưa, thêm reference này.

### 3.6. `MarkTogether.Client/AISettingsForm.cs` (+ `.Designer.cs`) — **TẠO MỚI**

Modal cấu hình. Layout (mockup):

```
┌────────────────────────────────────────────────┐
│  Cài đặt Chatbot AI                       [✕] │
├────────────────────────────────────────────────┤
│  Provider:  [Gemini ▾]   (chỉ Gemini ở v1)    │
│                                                │
│  Model:     [gemini-1.5-flash-latest ▾]       │
│                                                │
│  API key:   [••••••••••••••••••••••]  [Hiện]  │
│             Lấy tại: aistudio.google.com      │
│                                                │
│  [ Test kết nối ]   trạng thái: ✓ OK / ✗ lỗi  │
│                                                │
│            [ Huỷ ]            [ Lưu ]         │
└────────────────────────────────────────────────┘
```

Hành vi:
- `Load` → `AISettingsStore.Load()`, nạp combo + textbox (`PasswordChar='•'`).
- `btnToggleShow` → đổi `PasswordChar` ↔ `'\0'`.
- `btnTest` → gọi `SocketClient.Instance.AskAI("chat", "ping", "", null, txtKey.Text, cmbModel.SelectedItem.ToString(), "gemini")` trong `Task.Run`. Hiển thị `OK` (xanh) hoặc message lỗi (đỏ). **Không** lưu trước khi test.
- `btnSave` → `AISettingsStore.Save(new AISettings { Model=..., ApiKey=... })`, `DialogResult=OK`, đóng.
- `btnCancel` → bỏ thay đổi.

Style theo `AppTheme` + `UiFactory` (tham khảo `ShareDocumentForm.cs`).

### 3.7. `MarkTogether.Client/Network/SocketClient.cs` — mở rộng `AskAI`

Vị trí: line 543–556. Đổi chữ ký:

```csharp
public Payload_AI_Response AskAI(
    string mode, string userPrompt, string contextText,
    string docId = null,
    string apiKey = null, string model = null, string provider = null)
{
    EnsureAuthenticated();
    var response = Request(MessageType.AI_REQUEST,
        new Payload_AI_Request
        {
            docID       = docId,
            mode        = mode,
            userPrompt  = userPrompt,
            contextText = contextText,
            apiKey      = apiKey,
            model       = model,
            provider    = provider
        },
        timeoutMs: 45000);
    return ExtractOrThrow<Payload_AI_Response>(response, MessageType.AI_RESPONSE);
}
```

**Backward compat:** mọi caller cũ không truyền 3 tham số mới vẫn build được (default `null`). Server tự fallback.

### 3.8. `MarkTogether.Client/TypeRenderForm.Designer.cs` — thêm nút Settings

Trong block tab AI (line 326–368), thêm sau `btnAiSend`:

1. Declare ở cuối file (gần `private System.Windows.Forms.Button btnAiSend;`):
   ```csharp
   private System.Windows.Forms.Button btnAiSettings;
   ```
2. Trong `InitializeComponent`:
   ```csharp
   this.btnAiSettings = new System.Windows.Forms.Button();
   // ...
   this.pnlAiInput.Controls.Add(this.btnAiSettings);   // thêm sau dòng add btnAiSend
   // ...
   this.btnAiSettings.Dock = System.Windows.Forms.DockStyle.Right;
   this.btnAiSettings.Width = 90;
   this.btnAiSettings.Text = "⚙ Cài đặt";
   this.btnAiSettings.Name = "btnAiSettings";
   this.btnAiSettings.Click += new System.EventHandler(this.btnAiSettings_Click);
   ```

### 3.9. `MarkTogether.Client/TypeRenderForm.cs` — wire-up

1. Thêm `using MarkTogether.Client.Services;` (nếu chưa).

2. Thêm handler mới (đặt cạnh `btnAiSend_Click` line 1278):
   ```csharp
   private void btnAiSettings_Click(object sender, EventArgs e)
   {
       using (var dlg = new AISettingsForm())
       {
           dlg.ShowDialog(this);
       }
   }
   ```

3. Sửa `btnAiSend_Click` (line 1278) — đoạn gọi `AskAI` (line ~1299):

   Trước hàm `try`:
   ```csharp
   var aiCfg = AISettingsStore.Load();
   if (string.IsNullOrWhiteSpace(aiCfg.ApiKey))
   {
       var r = MessageBox.Show(
           "Bạn chưa cấu hình API key cho AI. Mở 'Cài đặt AI' bây giờ?",
           "Trợ lý AI", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
       if (r == DialogResult.Yes) btnAiSettings_Click(sender, e);
       return;
   }
   ```

   Sửa dòng gọi (line 1299):
   ```csharp
   var resp = await Task.Run(() =>
       SocketClient.Instance.AskAI(
           mode, prompt, ctx, _docId,
           aiCfg.ApiKey, aiCfg.Model, aiCfg.Provider));
   ```

---

## 4. Bảo mật & lưu ý

| Rủi ro | Mitigation |
|--------|-----------|
| API key đi qua TCP plaintext | Khuyến cáo TLS qua stunnel (xem DOCUMENTATION §11.6). Không phải scope task này nhưng phải nêu trong README/comment. |
| Server log lộ key | Hash SHA-256, lấy 8 hex đầu → log như `keyHash=ab12cd34`. **Không bao giờ** log raw `apiKey`. |
| User inject endpoint lạ qua `model` | Whitelist model strict trong `AISuggestionService` (mục §3.2 bước 3). |
| File `ai_settings.json` copy sang máy khác | DPAPI `CurrentUser` → decrypt fail → trả về `ApiKey=""` → user buộc nhập lại. |
| Replay attack hoặc nhiều client cùng key | Quota của Google AI Studio chịu, không phải vấn đề server. |
| Backward compat | Server fallback `provider/model/apiKey` rỗng → vẫn chạy nếu App.config có `GeminiApiKey`. Client cũ gọi `AskAI` 4 tham số vẫn compile (default `null`). |
| Repo đã commit key thật | **Phải revoke** key cũ ở https://aistudio.google.com/ sau khi merge. Đổi App.config về `""`. |

---

## 5. Acceptance criteria

- [ ] Build solution `MarkTogether.sln` **không lỗi** (0 error trên `Shared`, `Server`, `Client`).
- [ ] Mở `TypeRenderForm`, tab **Trợ lý AI** xuất hiện nút **⚙ Cài đặt**.
- [ ] Click nút → modal `AISettingsForm` mở ra, đọc setting cũ nếu có.
- [ ] Nhập key + chọn model `gemini-1.5-flash-latest` + bấm **Test** → hiện `OK` xanh trong ≤ 5 s.
- [ ] Bấm **Lưu** → file `%AppData%\MarkTogether\ai_settings.json` tồn tại; field `ApiKeyEncrypted` là chuỗi base64 (không phải plaintext key).
- [ ] Quay lại tab AI, gõ prompt, bấm **Gửi** → AI trả lời, server console log dòng `[AI] user=... model=gemini-1.5-flash-latest keyHash=xxxxxxxx mode=chat`.
- [ ] Đổi model sang `gemini-1.5-pro-latest`, lưu, gửi prompt → server log dòng mới với model mới.
- [ ] Xoá `ai_settings.json` → bấm **Gửi** trong tab AI → MessageBox "Bạn chưa cấu hình API key…" xuất hiện; **không** có request đi đến server.
- [ ] Nhập key sai → reply `success=false, message="API key không hợp lệ hoặc model sai."`
- [ ] App.config có `GeminiApiKey=""` (rỗng), client chưa cấu hình → MessageBox cảnh báo (không crash server).
- [ ] Server console **không** in raw `apiKey` ở bất kỳ log nào.

---

## 6. Test plan thủ công

| # | Tình huống | Kết quả mong đợi |
|---|------------|------------------|
| 1 | Build cả 3 project | 0 error |
| 2 | Chạy server không `GeminiApiKey`, client chưa cấu hình, gửi AI | MessageBox "Bạn chưa cấu hình…" |
| 3 | Cấu hình client key + `flash-latest`, test | `OK` |
| 4 | Đổi sang `pro-latest`, gửi prompt thật | Trả lời + server log đúng model |
| 5 | Key sai (`xxx`) | "API key không hợp lệ hoặc model sai." |
| 6 | Model lạ (sửa tay JSON thành `"gpt-4o"`) | "Model không được hỗ trợ: gpt-4o" |
| 7 | Mở `ai_settings.json` bằng notepad | Field `ApiKeyEncrypted` là base64 không đọc được |
| 8 | Copy `ai_settings.json` sang máy khác | Load về `ApiKey=""` (DPAPI fail nhưng không crash) |
| 9 | 2 user khác nhau trên cùng Windows | Mỗi user có file riêng theo `%AppData%` của profile |
| 10 | Throttle: gửi 2 prompt cách nhau 1 s | Cái thứ 2 reply "Vui lòng chờ vài giây…" (giữ behavior cũ) |

---

## 7. Out of scope (không làm ở task này)

- Lưu API key trên server (bảng `user_ai_settings`). Đẩy về Phase 2 nếu cần multi-device sync.
- Provider khác (OpenAI, Anthropic). Trường `provider` chỉ là placeholder.
- Web UI / admin panel để xem ai dùng AI bao nhiêu.
- Streaming response (Gemini streaming endpoint). Giữ non-stream cho đơn giản.
- TLS / stunnel cho TCP. Vẫn là khuyến cáo deploy, không phải code.

---

## 8. Thứ tự thực hiện đề xuất

1. **Shared** — sửa `Packet.cs` (3 field mới). Build Shared trước.
2. **Server** — sửa `AISuggestionService.cs`, `ClientHandler.HandleAiRequest`, `App.config`. Build Server.
3. **Client services** — tạo `Services/AISettingsStore.cs`, add reference `System.Security` nếu thiếu, build Client.
4. **Client UI** — tạo `AISettingsForm.cs` + Designer; sửa `TypeRenderForm.Designer.cs` (thêm `btnAiSettings`); sửa `TypeRenderForm.cs` (`btnAiSettings_Click`, sửa `btnAiSend_Click`). Build Client.
5. **Smoke test** chạy theo §6.

Mỗi bước build xong rồi mới sang bước tiếp theo để cô lập lỗi sớm.
