# Kế hoạch chi tiết — AI Editor Actions (cập nhật model + AI thao tác trực tiếp lên văn bản)

> **Tài liệu này nối tiếp** [`AI_CHATBOT_CONFIG_PLAN.md`](./AI_CHATBOT_CONFIG_PLAN.md) (đã triển khai xong: chọn model + tự nhập API key).
>
> Mục tiêu mới (2 phần độc lập, nhưng triển khai chung 1 nhánh để đỡ rebase):
>
> 1. **Cập nhật danh sách model** — thay 5 model cũ bằng 5 model mới: `gemini-2.5-flash`, `gemini-2.5-pro`, `gemini-2.5-flash-lite`, `gemini-3-flash`, `gemini-3.1-pro`.
> 2. **Action Mode** — AI không chỉ trả lời text, mà có thể "thực thi chỉnh sửa" trực tiếp lên `txtRawMarkdown` (uppercase, format heading, chuyển bullet → numbered, dịch đoạn bôi đen, v.v.). AI trả về **JSON có cấu trúc**; client preview rồi Apply / Reject; mọi thay đổi đi qua **OT pipeline hiện có** để đa người dùng vẫn đồng bộ.
>
> Tài liệu viết để AI / dev khác đọc và triển khai end-to-end mà không cần đọc thêm `DOCUMENTATION.md` hay code (mọi tham chiếu `file:line` đã ghi rõ).

---

## 1. Bối cảnh & baseline (chỗ nào KHÔNG đụng)

### 1.1. Hệ thống AI hiện tại (sau khi `AI_CHATBOT_CONFIG_PLAN.md` đã chạy)

| File | Vai trò hiện tại | Trạng thái cho task này |
|------|------------------|------------------------|
| `MarkTogether.Shared/Packet.cs` — `Payload_AI_Request` (line 646–657), `Payload_AI_Response` (line 659–664) | Schema: `docID, mode, userPrompt, contextText, provider, model, apiKey` (request) — `success, message, text` (response) | **PHẢI mở rộng** (thêm trường ngữ cảnh editor + trả về structured plan) |
| `MarkTogether.Server/Services/AISuggestionService.cs` (toàn bộ, 128 dòng) | Whitelist 5 model cũ (line 21–28), build prompt theo `mode` (line 107–126), gọi Gemini, trả plain text | **PHẢI sửa**: đổi whitelist + thêm prompt + JSON-only output cho `edit` mode |
| `MarkTogether.Server/Network/ClientHandler.cs` — `HandleAiRequest` (line 1625–1679) | Dispatcher `AI_REQUEST`, throttle 3 s, max 20 000 ký tự (prompt+ctx), gọi `AskAsync`, trả `Payload_AI_Response { success, text }` | **PHẢI sửa**: dispatch theo `actionMode`, gọi validator nếu là `edit`, raise giới hạn ký tự lên 80 000 cho edit mode |
| `MarkTogether.Client/Services/AISettingsStore.cs` — `SupportedModels` (line 19–26) | 5 model cũ + mặc định `gemini-1.5-flash-latest` | **PHẢI sửa**: thay 5 model mới + đổi default `gemini-2.5-flash` |
| `MarkTogether.Client/AISettingsForm.cs` — `btnTest_Click` (line 50–82), `AISettingsForm_Load` (line 30–42) | Test ping AI bằng `AskAI("chat", "ping", "", null, key, model, "gemini")` | **PHẢI sửa** chỉ ở chỗ default model index (xem §3.5) |
| `MarkTogether.Client/Network/SocketClient.cs` — `AskAI` (line ~543, đã mở rộng theo `AI_CHATBOT_CONFIG_PLAN.md` §3.7) | Wrapper gọi `MessageType.AI_REQUEST` | **PHẢI mở rộng**: thêm tham số `actionMode`, `documentText`, `selectionStart`, `selectionEnd`, `cursorPosition` |
| `MarkTogether.Client/TypeRenderForm.Designer.cs` — tab AI (line 326–376) | UI: `cmbAiMode` (4 mode: "Hỏi đáp"/"Tóm tắt"/"Viết tiếp"/"Dịch"), `txtAiPrompt`, `txtAiHistory`, `btnAiSend`, `btnAiSettings` | **PHẢI thêm**: checkbox `chkAiEditMode`, nút `btnAiUndo` |
| `MarkTogether.Client/TypeRenderForm.cs` — `btnAiSend_Click` (line 1287–1340) | Gửi AskAI, append text vào `txtAiHistory` | **PHẢI sửa**: nếu `chkAiEditMode` checked → gửi `actionMode="edit"`, xử lý structured response, mở preview dialog |

### 1.2. OT pipeline (KHÔNG đụng — chỉ tận dụng)

| Thành phần | File:line | Vai trò |
|-----------|-----------|---------|
| `txtRawMarkdown_TextChanged` | `TypeRenderForm.cs:462–477` | Mỗi lần `Text` đổi → `TrackRealtimeEditOps` |
| `TrackRealtimeEditOps` | `TypeRenderForm.cs:517–554` | So sánh old/new text, chia thành (delete, insert), enqueue qua `QueueOperation` |
| `ComputeTextDelta` | `TypeRenderForm.cs:556–574` | Diff prefix/suffix, trả về `{Position, DeletedText, InsertedText}` |
| `FlushPendingEditOperation` + `SendInsertOps`/`SendDeleteOps` | `TypeRenderForm.cs:699–705` | Gửi op qua `SocketClient` → server broadcast |

**Đây là điểm chốt:** khi `AIEditPlanApplier` tính ra `newFullText` và set `txtRawMarkdown.Text = newFullText` ở **đúng một lần**, `TrackRealtimeEditOps` tự sinh op (delete+insert) và broadcast cho mọi client còn lại. Chúng ta KHÔNG cần tự viết logic gửi op trong applier.

### 1.3. KHÔNG đụng vào

- `OT/*`, `Database/*`, `SocketServer`, `SessionManager`, `DocumentPermissionService` (trừ đọc permission để gate AI edit cho `viewer`).
- Các handler khác trong `ClientHandler.cs` (chỉ `HandleAiRequest`).
- Logic comment, share, login, register, version history.

---

## 2. Quyết định kiến trúc

### 2.1. Cập nhật danh sách model (mục tiêu #1)

5 model whitelist mới (thay nguyên block cũ):

```
gemini-2.5-flash          ← mặc định
gemini-2.5-pro
gemini-2.5-flash-lite
gemini-3-flash
gemini-3.1-pro
```

> **Lưu ý dev:** `gemini-3-flash` và `gemini-3.1-pro` là model do user yêu cầu whitelist. Nếu Google AI Studio chưa public chúng khi triển khai, server vẫn forward — Gemini sẽ trả `400` → message hiển thị user `"API key không hợp lệ hoặc model sai."` (logic xử lý 400 đã có sẵn ở `AISuggestionService.cs:87`). Không cần special-case.

Đồng bộ ở **2 nơi**:
- `MarkTogether.Server/Services/AISuggestionService.cs:21–28` — `SupportedModels` (HashSet)
- `MarkTogether.Client/Services/AISettingsStore.cs:19–26` — `SupportedModels` (IReadOnlyList) + đổi `Model` default ở line 13

### 2.2. Hai chế độ AI (mục tiêu #2)

| Chế độ | `actionMode` (mới) | UI toggle | Hành vi |
|--------|--------------------|-----------|---------|
| **Text** (cũ) | `"text"` | Mặc định, `chkAiEditMode` UNchecked | AI trả `text` → append vào `txtAiHistory` (giữ nguyên behavior hiện tại) |
| **Edit** (mới) | `"edit"` | `chkAiEditMode` checked | AI trả JSON theo schema cố định → server validate → client preview → user Apply/Reject |

User vẫn dùng `cmbAiMode` cũ ("Hỏi đáp"/"Tóm tắt"/"Viết tiếp"/"Dịch") cho text mode. Khi bật `chkAiEditMode`, `cmbAiMode` bị disable — vì edit mode dùng prompt riêng (xem §2.5).

### 2.3. Envelope JSON cho Action Mode

**1 schema duy nhất**, AI bắt buộc trả về:

```json
{
  "type": "edit_plan",
  "summary": "Mô tả ngắn gọn việc sẽ làm",
  "target": "selection" | "document",
  "patches": [
    { "op": "replace", "start": 120, "end": 250, "newText": "..." },
    { "op": "insert",  "start": 500, "newText": "..." },
    { "op": "delete",  "start": 700, "end": 720 }
  ],
  "notes": "Tuỳ chọn — giải thích thêm cho user"
}
```

Hoặc (khi cần ghi đè toàn bộ — uppercase doc, format lại heading toàn bộ):

```json
{
  "type": "rewrite_document",
  "summary": "Viết hoa toàn bộ văn bản",
  "newContent": "...",
  "notes": "..."
}
```

Hoặc khi AI không thể thực hiện (yêu cầu mơ hồ / vi phạm safety):

```json
{
  "type": "chat",
  "message": "Yêu cầu chưa rõ. Bạn muốn viết hoa cả văn bản hay chỉ phần đang chọn?"
}
```

**Chỉ 3 `type` được chấp nhận**: `edit_plan` | `rewrite_document` | `chat`. Mọi `type` khác → reject.

**Tại sao dùng cả patches lẫn rewrite_document?**
- `edit_plan` (patches) — nhẹ, chỉ gửi đoạn thay đổi, OT diff nhỏ. Dùng cho selection hoặc edit cục bộ.
- `rewrite_document` — đơn giản cho transform toàn bộ doc (uppercase, format markdown), khỏi tính position. Trade-off: OT diff to hơn nhưng vẫn chạy. AI ít sai position vì không phải nghĩ ra số.

### 2.4. Áp dụng patch qua OT pipeline

`AIEditPlanApplier.Apply(plan, oldText)` → trả về `newText` (string). Sau đó client:

```csharp
_suppressOpTracking = false;          // đảm bảo OT bắt được diff
txtRawMarkdown.Text = newText;        // 1 lần duy nhất
// TextChanged → TrackRealtimeEditOps → diff → SendInsertOps/SendDeleteOps tự động
```

**Thuật toán Apply:**

1. Nếu `type == "rewrite_document"` → trả về `plan.newContent` (sau khi validate độ dài).
2. Nếu `type == "edit_plan"`:
   - Sort `patches` theo `start` **giảm dần** (apply từ cuối → đầu, position không bị shift).
   - Validate **không overlap**: với mọi cặp `p_i, p_j` (sau khi sort), `p_i.end <= p_j.start` (so theo thứ tự gốc).
   - Áp từng patch:
     - `replace`: `text = text[..start] + newText + text[end..]`
     - `insert`: `text = text[..start] + newText + text[start..]`
     - `delete`: `text = text[..start] + text[end..]`
   - Trả về `text` cuối cùng.

**Concurrency note:** giữa lúc gửi request đi và lúc Apply, user khác có thể đã sửa doc → `oldText` mà AI thấy != `txtRawMarkdown.Text` hiện tại. Trước khi Apply, so sánh: nếu `txtRawMarkdown.Text` đã đổi → cảnh báo `"Tài liệu đã thay đổi từ lúc AI phân tích. Vẫn áp dụng?"` (Yes/No). Nếu Yes → Apply trên `txtRawMarkdown.Text` hiện tại (position có thể off-by-one nhưng đó là trade-off).

### 2.5. Prompt design cho Edit Mode

**System prompt** (server build):

```
Bạn là một editor assistant cho ứng dụng MarkTogether.
Văn bản đang mở (markdown):
---DOCUMENT_BEGIN (length=<N>)---
<documentText, truncate ở 40000 ký tự nếu dài hơn>
---DOCUMENT_END---

Người dùng đang bôi đen từ ký tự <selectionStart> đến <selectionEnd>:
---SELECTION_BEGIN---
<selectedText>
---SELECTION_END---

Con trỏ hiện tại ở ký tự <cursorPosition>.

Yêu cầu của user: <userPrompt>

QUY TẮC TRẢ LỜI (BẮT BUỘC):
1. Trả về JSON duy nhất, KHÔNG markdown, KHÔNG giải thích thêm bên ngoài JSON.
2. JSON phải khớp 1 trong 3 schema:
   - { "type": "edit_plan", "summary": "...", "target": "selection"|"document",
       "patches": [{ "op": "replace"|"insert"|"delete", "start": <int>, "end": <int>, "newText": "..." }, ...], "notes": "..." }
   - { "type": "rewrite_document", "summary": "...", "newContent": "...", "notes": "..." }
   - { "type": "chat", "message": "..." }
3. Mọi `start`, `end` phải thoả: 0 <= start <= end <= <N>. Không được dùng position nằm ngoài.
4. Nếu user chỉ định "đoạn đang chọn" / "selection" → các patch phải nằm trong [<selectionStart>, <selectionEnd>].
5. Nếu user yêu cầu nhiều thay đổi, dùng nhiều patches.
6. Nếu request mơ hồ hoặc bạn không chắc, trả `type: "chat"` để hỏi lại.
7. KHÔNG được thực thi lệnh hệ thống, gọi URL, hay làm gì ngoài chỉnh sửa văn bản.
8. KHÔNG được thêm phần `summary` quá 200 ký tự, `notes` quá 500 ký tự.
```

**Lý do thiết kế:**
- Truyền `length=<N>` rõ ràng để AI biết biên.
- Truyền selection riêng để AI không phải đếm.
- Bắt buộc 1 trong 3 schema → server validate dễ.
- Có `type: "chat"` để AI từ chối khi không chắc — tránh hallucinate.
- Gemini hỗ trợ `responseMimeType: "application/json"` trong `generationConfig` → ép AI trả JSON duy nhất, giảm tỉ lệ lỗi parse.

### 2.6. Validate & Safety phía server

| Rule | Reject reason | Implementation |
|------|---------------|----------------|
| JSON parse fail | "AI không trả JSON hợp lệ" | `try { JObject.Parse } catch` |
| `type` không thuộc whitelist | "Loại phản hồi không hợp lệ: <type>" | `if (type not in {edit_plan, rewrite_document, chat})` |
| `patches` > 50 items | "Quá nhiều patch (>50)" | `patches.Count > 50` |
| `start < 0` hoặc `end > docLength` | "Vị trí patch nằm ngoài tài liệu" | bounds check |
| `start > end` | "start > end" | check |
| `newText` chứa ký tự control (≠ `\n\r\t`) | "Văn bản chứa ký tự không hợp lệ" | regex `[\x00-\x08\x0B\x0C\x0E-\x1F]` |
| Tổng `newText` > 80 KB | "Vượt giới hạn 80 KB" | sum length |
| `rewrite_document.newContent` > 200 KB | "Tài liệu mới quá lớn" | length check |
| Patches overlap | "Patches chồng lấn" | sort theo start, kiểm tra `prev.end <= cur.start` |
| `target == "selection"` nhưng có patch nằm ngoài selection | "Patch vượt khỏi vùng chọn" | check inside `[selectionStart, selectionEnd]` |

Khi reject, server **không** trả lỗi cứng — trả về:
```json
{ "success": true, "kind": "text", "text": "AI trả về kế hoạch không hợp lệ: <reason>. Bạn có thể thử lại hoặc diễn đạt rõ hơn." }
```
→ Client hiện trong `txtAiHistory`, user thấy được lý do, không bị mất state.

### 2.7. Undo / Preview / Apply (client UX)

- **Preview**: trước khi Apply, mở modal `AIEditPreviewForm`:
  - Hiển thị `plan.summary`, `plan.notes`, số patches.
  - Hiển thị diff: dùng layout 2 cột — **trái** = `oldText` (highlight đoạn bị thay đổi màu đỏ nhạt), **phải** = `newText` (highlight đoạn mới màu xanh nhạt). Cuộn đồng bộ.
  - Cho `rewrite_document`: dùng line-based diff đơn giản (so sánh từng dòng, đánh dấu line đổi). Để giảm phụ thuộc, **không** kéo DiffMatchPatch — tự viết hàm `LineDiff` thuần (xem §3.10).
  - Buttons: `[ Áp dụng ]` (primary) — `[ Từ chối ]` — `[ Lưu vào lịch sử AI ]` (chỉ append vào `txtAiHistory` mà không apply).

- **Apply**:
  1. Snapshot `txtRawMarkdown.Text` vào stack `_aiUndoStack` (giới hạn 10 entry).
  2. `txtRawMarkdown.Text = newText` → OT pipeline tự broadcast.
  3. Append `txtAiHistory`: `"AI ✓ Đã áp dụng: <summary>"`.

- **Undo**:
  - Nút `btnAiUndo` trên tab AI (kế `btnAiSettings`). Disable khi `_aiUndoStack` rỗng.
  - Pop top, set `txtRawMarkdown.Text = popped` → OT broadcast diff ngược.
  - Append `txtAiHistory`: `"AI ↶ Hoàn tác."`.
  - **Ctrl+Z native** vẫn hoạt động cho TextBox (RichTextBox?) — đây là **stack bổ sung** giúp hoàn tác toàn bộ patch trong 1 click thay vì spam Ctrl+Z.

- **Gate quyền edit**:
  - Trước khi gửi request `actionMode="edit"`: nếu `txtRawMarkdown.ReadOnly` → MessageBox `"Bạn chỉ có quyền xem, không thể dùng AI để sửa văn bản."` → không gửi.
  - Trước khi Apply: re-check `ReadOnly` (đề phòng permission đổi giữa chừng).

### 2.8. Backward compatibility

- `Payload_AI_Request` thêm trường mới (nullable / default 0) — client cũ vẫn build được (giữ behavior cũ vì `actionMode` rỗng → server fallback `"text"`).
- `Payload_AI_Response` thêm trường `kind` và `editPlanJson`. Client cũ chỉ đọc `text` → vẫn chạy.
- `SocketClient.AskAI` mở rộng với tham số default `null/0` → caller cũ (như `btnTest_Click` ở `AISettingsForm.cs:67`) không cần đụng.

---

## 3. Thay đổi chi tiết từng file

### 3.1. `MarkTogether.Shared/Packet.cs` — mở rộng payload

**Vị trí:** line 646–664 (block AI).

Thay nguyên block `Payload_AI_Request` và `Payload_AI_Response`:

```csharp
public class Payload_AI_Request
{
    public string docID { get; set; }
    public string mode { get; set; }          // "chat" | "summarize" | "continue" | "translate" (cũ — vẫn dùng cho text mode)
    public string userPrompt { get; set; }
    public string contextText { get; set; }

    // Cấu hình AI động phía client
    public string provider { get; set; }      // "gemini"
    public string model { get; set; }
    public string apiKey { get; set; }

    // === MỚI: Editor context cho Action Mode ===
    public string actionMode { get; set; }    // "text" (mặc định) | "edit"
    public string documentText { get; set; }  // toàn bộ markdown — chỉ gửi khi actionMode="edit"
    public int selectionStart { get; set; }   // 0 nếu không có selection
    public int selectionEnd { get; set; }     // 0 nếu không có selection
    public int cursorPosition { get; set; }
}

public class Payload_AI_Response
{
    public bool success { get; set; }
    public string message { get; set; }
    public string text { get; set; }          // text reply (text mode) HOẶC fallback message khi edit mode lỗi

    // === MỚI ===
    public string kind { get; set; }          // "text" | "edit_plan"
    public string editPlanJson { get; set; }  // JSON string đã validate; rỗng nếu kind != "edit_plan"
}
```

> **Tại sao `editPlanJson` là string thay vì nested object?** Để giữ Newtonsoft đơn giản (không lo về polymorphic deserialization), và để server validate xong rồi serialize lại đúng schema chuẩn. Client tự `JObject.Parse` để render preview.

### 3.2. `MarkTogether.Server/Services/AISuggestionService.cs` — đổi whitelist + thêm edit mode

#### 3.2.1. Đổi `SupportedModels` (line 21–28)

```csharp
private static readonly HashSet<string> SupportedModels = new HashSet<string>
{
    "gemini-2.5-flash",
    "gemini-2.5-pro",
    "gemini-2.5-flash-lite",
    "gemini-3-flash",
    "gemini-3.1-pro"
};
```

#### 3.2.2. Đổi default model (line 45–47)

```csharp
model = string.IsNullOrWhiteSpace(model)
    ? "gemini-2.5-flash"
    : model.Trim();
```

#### 3.2.3. Thêm method `AskEditAsync`

Thêm method mới (không thay đổi `AskAsync` cũ):

```csharp
public static async Task<string> AskEditAsync(
    string provider, string model, string apiKey,
    string userPrompt,
    string documentText, int documentLength,
    int selectionStart, int selectionEnd, int cursorPosition)
{
    // (giống AskAsync) — validate provider/model/apiKey
    provider = string.IsNullOrWhiteSpace(provider) ? "gemini" : provider.Trim().ToLowerInvariant();
    if (provider != "gemini")
        throw new InvalidOperationException("Provider chưa được hỗ trợ: " + provider);

    model = string.IsNullOrWhiteSpace(model) ? "gemini-2.5-flash" : model.Trim();
    if (!SupportedModels.Contains(model))
        throw new InvalidOperationException("Model không được hỗ trợ: " + model);

    if (string.IsNullOrWhiteSpace(apiKey))
        apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("Chưa cấu hình API key. Mở 'Cài đặt AI' để nhập.");

    string prompt = BuildEditPrompt(userPrompt, documentText, documentLength,
                                    selectionStart, selectionEnd, cursorPosition);

    var requestBody = new
    {
        contents = new[]
        {
            new { role = "user", parts = new[] { new { text = prompt } } }
        },
        generationConfig = new
        {
            temperature = 0.2,            // thấp hơn chat (0.4) để giảm hallucinate
            maxOutputTokens = 8192,       // đủ cho rewrite_document trung bình
            responseMimeType = "application/json"  // ép Gemini trả JSON-only
        }
    };

    string json = JsonConvert.SerializeObject(requestBody);
    string url = $"{BaseUrl}{model}:generateContent?key={apiKey}";

    using (var req = new HttpRequestMessage(HttpMethod.Post, url))
    {
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using (var res = await _http.SendAsync(req).ConfigureAwait(false))
        {
            string body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                string msg = (int)res.StatusCode == 400 ? "API key không hợp lệ hoặc model sai."
                    : (int)res.StatusCode == 429 ? "Đã vượt quota của API key này."
                    : $"Gemini lỗi {(int)res.StatusCode}: {Truncate(body, 200)}";
                throw new InvalidOperationException(msg);
            }

            var jo = JObject.Parse(body);
            string text = jo["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
            return string.IsNullOrEmpty(text) ? "{}" : text.Trim();
        }
    }
}

private static string BuildEditPrompt(
    string userPrompt, string documentText, int documentLength,
    int selectionStart, int selectionEnd, int cursorPosition)
{
    string truncatedDoc = documentText ?? "";
    if (truncatedDoc.Length > 40000)
        truncatedDoc = truncatedDoc.Substring(0, 40000) + "\n…(đã cắt bớt — tổng " + documentLength + " ký tự)";

    string selectedText = "";
    if (selectionStart >= 0 && selectionEnd > selectionStart && selectionEnd <= documentLength)
        selectedText = documentText.Substring(selectionStart,
            Math.Min(selectionEnd - selectionStart, 5000));

    return
$@"Bạn là editor assistant cho ứng dụng MarkTogether.
Văn bản đang mở (markdown):
---DOCUMENT_BEGIN (length={documentLength})---
{truncatedDoc}
---DOCUMENT_END---

Người dùng đang bôi đen từ ký tự {selectionStart} đến {selectionEnd}:
---SELECTION_BEGIN---
{selectedText}
---SELECTION_END---

Con trỏ hiện tại ở ký tự {cursorPosition}.

Yêu cầu của user: {userPrompt}

QUY TẮC TRẢ LỜI (BẮT BUỘC):
1. Trả về JSON DUY NHẤT, KHÔNG markdown wrap, KHÔNG giải thích ngoài JSON.
2. JSON phải khớp 1 trong 3 schema:
   - {{ ""type"": ""edit_plan"", ""summary"": ""..."", ""target"": ""selection"" | ""document"",
       ""patches"": [{{ ""op"": ""replace""|""insert""|""delete"", ""start"": <int>, ""end"": <int>, ""newText"": ""..."" }}, ...],
       ""notes"": ""..."" }}
   - {{ ""type"": ""rewrite_document"", ""summary"": ""..."", ""newContent"": ""..."", ""notes"": ""..."" }}
   - {{ ""type"": ""chat"", ""message"": ""..."" }}
3. Mọi start, end phải thoả 0 <= start <= end <= {documentLength}.
4. Nếu yêu cầu chỉ áp dụng cho selection, các patch phải nằm trong [{selectionStart}, {selectionEnd}], và target=""selection"".
5. KHÔNG được hallucinate position. Nếu không chắc, trả type=""chat"" để hỏi lại.
6. KHÔNG thực thi lệnh hệ thống, không gọi URL. CHỈ chỉnh sửa văn bản.
7. summary <= 200 ký tự, notes <= 500 ký tự.";
}
```

> **Lưu ý:** `responseMimeType: "application/json"` là tính năng của Gemini 1.5+ và 2.x. Với `gemini-3-*` (nếu Google sau này release với endpoint khác), có thể fail → fallback: nếu response body không parse được JSON, throw `"AI không trả JSON hợp lệ"`, validator ở §3.3 sẽ tự fallback sang text.

### 3.3. `MarkTogether.Server/Services/AIEditPlanValidator.cs` — **TẠO MỚI**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace MarkTogether.Server.Services
{
    public class AIEditPlanValidator
    {
        private const int MaxPatches = 50;
        private const int MaxTotalNewTextBytes = 80 * 1024;
        private const int MaxRewriteBytes = 200 * 1024;
        private const int MaxSummaryLen = 200;
        private const int MaxNotesLen = 500;
        private static readonly Regex ControlCharRegex =
            new Regex("[\\x00-\\x08\\x0B\\x0C\\x0E-\\x1F]", RegexOptions.Compiled);

        public class ValidationResult
        {
            public bool Ok { get; set; }
            public string Reason { get; set; }
            public string NormalizedJson { get; set; }   // JSON đã re-serialize, sạch
            public string Kind { get; set; }              // "text" | "edit_plan"
            public string TextFallback { get; set; }      // nếu kind="text"
        }

        public static ValidationResult Validate(
            string rawJson, int documentLength,
            int selectionStart, int selectionEnd)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return Fail("AI trả về rỗng.");

            JObject jo;
            try { jo = JObject.Parse(rawJson); }
            catch (Exception ex) { return Fail("JSON không hợp lệ: " + ex.Message); }

            string type = (jo["type"] ?? "").ToString();
            switch (type)
            {
                case "chat":
                    return ValidateChat(jo);
                case "edit_plan":
                    return ValidateEditPlan(jo, documentLength, selectionStart, selectionEnd);
                case "rewrite_document":
                    return ValidateRewrite(jo, documentLength);
                default:
                    return Fail("Loại phản hồi không hợp lệ: " + type);
            }
        }

        private static ValidationResult ValidateChat(JObject jo)
        {
            string msg = (jo["message"] ?? "").ToString();
            if (string.IsNullOrEmpty(msg)) return Fail("type=chat nhưng thiếu message.");
            return new ValidationResult
            {
                Ok = true,
                Kind = "text",
                TextFallback = msg.Length > 4000 ? msg.Substring(0, 4000) : msg
            };
        }

        private static ValidationResult ValidateEditPlan(
            JObject jo, int docLen, int selStart, int selEnd)
        {
            string summary = (jo["summary"] ?? "").ToString();
            if (summary.Length > MaxSummaryLen) return Fail("summary quá dài.");

            string target = (jo["target"] ?? "document").ToString();
            if (target != "selection" && target != "document")
                return Fail("target không hợp lệ: " + target);

            var patches = jo["patches"] as JArray;
            if (patches == null || patches.Count == 0)
                return Fail("edit_plan không có patches.");
            if (patches.Count > MaxPatches)
                return Fail("Quá nhiều patches: " + patches.Count);

            var parsed = new List<(string op, int start, int end, string newText)>();
            int totalNewBytes = 0;

            foreach (var p in patches)
            {
                string op = (p["op"] ?? "").ToString();
                if (op != "replace" && op != "insert" && op != "delete")
                    return Fail("op không hợp lệ: " + op);

                int start = (p["start"] ?? -1).Value<int>();
                int end = (p["end"] ?? start).Value<int>();
                string newText = (p["newText"] ?? "").ToString();

                if (op == "insert") end = start;  // insert: end ép = start

                if (start < 0 || start > docLen) return Fail($"start={start} ngoài [0,{docLen}].");
                if (end < start || end > docLen) return Fail($"end={end} không hợp lệ.");
                if (target == "selection" && (start < selStart || end > selEnd))
                    return Fail($"Patch [{start},{end}] vượt selection [{selStart},{selEnd}].");

                if (ControlCharRegex.IsMatch(newText))
                    return Fail("newText chứa ký tự control.");

                totalNewBytes += System.Text.Encoding.UTF8.GetByteCount(newText);
                if (totalNewBytes > MaxTotalNewTextBytes)
                    return Fail("Tổng newText vượt 80 KB.");

                parsed.Add((op, start, end, newText));
            }

            // Kiểm overlap: sort theo start, mỗi prev.end <= cur.start
            var sorted = parsed.OrderBy(x => x.start).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i - 1].end > sorted[i].start)
                    return Fail($"Patches chồng lấn tại ~{sorted[i].start}.");
            }

            // Re-serialize sạch (loại bỏ field thừa do AI sinh)
            var clean = new JObject
            {
                ["type"] = "edit_plan",
                ["summary"] = summary,
                ["target"] = target,
                ["patches"] = new JArray(parsed.Select(p => new JObject
                {
                    ["op"] = p.op,
                    ["start"] = p.start,
                    ["end"] = p.end,
                    ["newText"] = p.newText
                })),
                ["notes"] = TruncateNotes(jo["notes"]?.ToString())
            };
            return new ValidationResult { Ok = true, Kind = "edit_plan", NormalizedJson = clean.ToString() };
        }

        private static ValidationResult ValidateRewrite(JObject jo, int docLen)
        {
            string newContent = (jo["newContent"] ?? "").ToString();
            int bytes = System.Text.Encoding.UTF8.GetByteCount(newContent);
            if (bytes > MaxRewriteBytes) return Fail("newContent quá lớn (>200KB).");
            if (ControlCharRegex.IsMatch(newContent)) return Fail("newContent chứa ký tự control.");

            string summary = (jo["summary"] ?? "").ToString();
            if (summary.Length > MaxSummaryLen) return Fail("summary quá dài.");

            var clean = new JObject
            {
                ["type"] = "rewrite_document",
                ["summary"] = summary,
                ["newContent"] = newContent,
                ["notes"] = TruncateNotes(jo["notes"]?.ToString())
            };
            return new ValidationResult { Ok = true, Kind = "edit_plan", NormalizedJson = clean.ToString() };
        }

        private static string TruncateNotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= MaxNotesLen ? s : s.Substring(0, MaxNotesLen);
        }

        private static ValidationResult Fail(string reason) =>
            new ValidationResult { Ok = false, Reason = reason };
    }
}
```

Thêm `<Compile Include="Services\AIEditPlanValidator.cs" />` vào `Server.csproj`.

### 3.4. `MarkTogether.Server/Network/ClientHandler.cs` — `HandleAiRequest` dispatch

**Vị trí:** line 1625–1679. Sửa từ `var p = packet.GetPayload<Payload_AI_Request>();` (line 1640) trở xuống:

```csharp
var p = packet.GetPayload<Payload_AI_Request>();
if (p == null)
{
    Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
    { success = false, message = "Payload rỗng." });
    return;
}

string actionMode = string.IsNullOrWhiteSpace(p.actionMode) ? "text" : p.actionMode.Trim().ToLowerInvariant();

// Validate giới hạn theo mode
int maxBytes = actionMode == "edit" ? 80_000 : 20_000;
int totalLen = (p.userPrompt?.Length ?? 0) + (p.contextText?.Length ?? 0) + (p.documentText?.Length ?? 0);
if (totalLen > maxBytes)
{
    Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
    { success = false, message = $"Prompt quá dài (>{maxBytes} ký tự)." });
    return;
}

if (actionMode == "text" &&
    string.IsNullOrWhiteSpace(p.userPrompt) && string.IsNullOrWhiteSpace(p.contextText))
{
    Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
    { success = false, message = "Prompt rỗng." });
    return;
}
if (actionMode == "edit" && string.IsNullOrWhiteSpace(p.userPrompt))
{
    Reply(packet, MessageType.AI_RESPONSE, new Payload_AI_Response
    { success = false, message = "Cần mô tả thao tác cần thực hiện." });
    return;
}

string keyHash = string.IsNullOrEmpty(p.apiKey) ? "(server-fallback)" : Sha256Short(p.apiKey);
Console.WriteLine($"[AI] user={currentUserId} provider={p.provider ?? "gemini"} " +
                  $"model={p.model ?? "default"} keyHash={keyHash} actionMode={actionMode} " +
                  $"docLen={p.documentText?.Length ?? 0}");

Packet originPacket = packet;
Task.Run(async () =>
{
    try
    {
        if (actionMode == "edit")
        {
            string rawJson = await AISuggestionService.AskEditAsync(
                p.provider, p.model, p.apiKey,
                p.userPrompt,
                p.documentText ?? "", p.documentText?.Length ?? 0,
                p.selectionStart, p.selectionEnd, p.cursorPosition).ConfigureAwait(false);

            var vr = AIEditPlanValidator.Validate(
                rawJson, p.documentText?.Length ?? 0, p.selectionStart, p.selectionEnd);

            if (!vr.Ok)
            {
                // Fallback sang text — không hard-fail
                Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
                {
                    success = true,
                    kind = "text",
                    text = "AI trả về kế hoạch không hợp lệ: " + vr.Reason +
                           ". Bạn có thể thử lại hoặc diễn đạt rõ hơn."
                });
                return;
            }

            Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
            {
                success = true,
                kind = vr.Kind,                                // "text" (nếu AI chọn chat) hoặc "edit_plan"
                text = vr.TextFallback ?? "",                  // chat message khi kind="text"
                editPlanJson = vr.NormalizedJson ?? ""         // JSON sạch khi kind="edit_plan"
            });
        }
        else
        {
            // === Text mode cũ ===
            string text = await AISuggestionService.AskAsync(
                p.provider, p.model, p.apiKey,
                p.mode, p.userPrompt, p.contextText).ConfigureAwait(false);
            Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
            { success = true, kind = "text", text = text });
        }
    }
    catch (Exception ex)
    {
        Reply(originPacket, MessageType.AI_RESPONSE, new Payload_AI_Response
        { success = false, message = ex.Message });
    }
});
```

Giữ nguyên throttle 3 s (line 1631–1638) — đặt phía trên block này.

### 3.5. `MarkTogether.Client/Services/AISettingsStore.cs` — đổi danh sách model

**Vị trí:** line 13 và line 19–26.

```csharp
public class AISettings
{
    public string Provider { get; set; } = "gemini";
    public string Model { get; set; } = "gemini-2.5-flash";   // ← đổi default
    public string ApiKey { get; set; } = "";
}

public static class AISettingsStore
{
    public static readonly IReadOnlyList<string> SupportedModels = new[]
    {
        "gemini-2.5-flash",
        "gemini-2.5-pro",
        "gemini-2.5-flash-lite",
        "gemini-3-flash",
        "gemini-3.1-pro",
    };
    // ... phần còn lại giữ nguyên
}
```

Cũng đổi default trong `Load()` (line 50–53) nếu cần — fallback hiện đang `"gemini-1.5-flash-latest"`:

```csharp
Model = string.IsNullOrEmpty(dto.Model) ? "gemini-2.5-flash" : dto.Model,
```

Tương tự ở `Save()` (line 67–69).

### 3.6. `MarkTogether.Client/Services/AIEditPlanModels.cs` — **TẠO MỚI**

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarkTogether.Client.Services
{
    public class AiEditPlan
    {
        [JsonProperty("type")]    public string Type { get; set; }       // "edit_plan" | "rewrite_document" | "chat"
        [JsonProperty("summary")] public string Summary { get; set; }
        [JsonProperty("target")]  public string Target { get; set; }     // "selection" | "document"
        [JsonProperty("patches")] public List<AiEditPatch> Patches { get; set; }
        [JsonProperty("newContent")] public string NewContent { get; set; }
        [JsonProperty("notes")]   public string Notes { get; set; }
        [JsonProperty("message")] public string Message { get; set; }    // chỉ dùng khi type="chat"
    }

    public class AiEditPatch
    {
        [JsonProperty("op")]      public string Op { get; set; }         // "replace" | "insert" | "delete"
        [JsonProperty("start")]   public int Start { get; set; }
        [JsonProperty("end")]     public int End { get; set; }
        [JsonProperty("newText")] public string NewText { get; set; }
    }
}
```

Thêm `<Compile Include="Services\AIEditPlanModels.cs" />` vào `Client.csproj`.

### 3.7. `MarkTogether.Client/Services/AIEditPlanApplier.cs` — **TẠO MỚI**

```csharp
using System;
using System.Linq;

namespace MarkTogether.Client.Services
{
    public static class AIEditPlanApplier
    {
        public class ApplyResult
        {
            public bool Ok { get; set; }
            public string NewText { get; set; }
            public string Error { get; set; }
        }

        public static ApplyResult Apply(string oldText, AiEditPlan plan)
        {
            if (plan == null) return new ApplyResult { Ok = false, Error = "Plan rỗng." };
            oldText = oldText ?? "";

            if (plan.Type == "rewrite_document")
                return new ApplyResult { Ok = true, NewText = plan.NewContent ?? "" };

            if (plan.Type != "edit_plan")
                return new ApplyResult { Ok = false, Error = "Plan không có patches để apply (type=" + plan.Type + ")." };

            if (plan.Patches == null || plan.Patches.Count == 0)
                return new ApplyResult { Ok = false, Error = "Không có patches." };

            // Sort giảm dần theo start → áp từ cuối lên đầu, position không shift.
            var sorted = plan.Patches.OrderByDescending(p => p.Start).ToList();
            string text = oldText;
            foreach (var p in sorted)
            {
                if (p.Start < 0 || p.Start > text.Length || p.End < p.Start || p.End > text.Length)
                    return new ApplyResult { Ok = false, Error = $"Patch [{p.Start},{p.End}] vượt biên (text length={text.Length})." };

                switch (p.Op)
                {
                    case "replace":
                        text = text.Substring(0, p.Start) + (p.NewText ?? "") + text.Substring(p.End);
                        break;
                    case "insert":
                        text = text.Substring(0, p.Start) + (p.NewText ?? "") + text.Substring(p.Start);
                        break;
                    case "delete":
                        text = text.Substring(0, p.Start) + text.Substring(p.End);
                        break;
                    default:
                        return new ApplyResult { Ok = false, Error = "Op không hỗ trợ: " + p.Op };
                }
            }
            return new ApplyResult { Ok = true, NewText = text };
        }
    }
}
```

Thêm `<Compile Include="Services\AIEditPlanApplier.cs" />` vào `Client.csproj`.

### 3.8. `MarkTogether.Client/Network/SocketClient.cs` — mở rộng `AskAI`

**Vị trí:** method `AskAI` ở line ~543 (sau khi `AI_CHATBOT_CONFIG_PLAN.md` §3.7 đã sửa).

Đổi chữ ký, thêm 5 tham số default:

```csharp
public Payload_AI_Response AskAI(
    string mode, string userPrompt, string contextText,
    string docId = null,
    string apiKey = null, string model = null, string provider = null,
    // === MỚI ===
    string actionMode = null,
    string documentText = null,
    int selectionStart = 0, int selectionEnd = 0, int cursorPosition = 0)
{
    EnsureAuthenticated();
    var response = Request(MessageType.AI_REQUEST,
        new Payload_AI_Request
        {
            docID = docId,
            mode = mode,
            userPrompt = userPrompt,
            contextText = contextText,
            apiKey = apiKey,
            model = model,
            provider = provider,
            actionMode = actionMode,
            documentText = documentText,
            selectionStart = selectionStart,
            selectionEnd = selectionEnd,
            cursorPosition = cursorPosition
        },
        timeoutMs: 60000);   // edit mode chậm hơn — raise lên 60s
    return ExtractOrThrow<Payload_AI_Response>(response, MessageType.AI_RESPONSE);
}
```

**Caller cũ vẫn không bị break** — `btnTest_Click` ở `AISettingsForm.cs:67` không truyền các tham số mới → default → `actionMode=null` → server fallback `"text"`.

### 3.9. `MarkTogether.Client/TypeRenderForm.Designer.cs` — thêm UI

**Vị trí:** block `// AI tab` (line 326–376).

Sau `btnAiSettings` (line 372–376), thêm:

1. **Declare** (đặt gần `private System.Windows.Forms.Button btnAiSettings;` — thường ở cuối Designer):
   ```csharp
   private System.Windows.Forms.CheckBox chkAiEditMode;
   private System.Windows.Forms.Button btnAiUndo;
   ```

2. **InitializeComponent** — thêm trong block `pnlAiInput` (sau line 351):
   ```csharp
   this.pnlAiInput.Controls.Add(this.chkAiEditMode);
   this.pnlAiInput.Controls.Add(this.btnAiUndo);
   ```

3. **Khai báo control** (đặt sau khối `btnAiSettings`, trước `// Form`):
   ```csharp
   //
   this.chkAiEditMode.Dock = System.Windows.Forms.DockStyle.Top;
   this.chkAiEditMode.Text = "AI có thể chỉnh sửa văn bản (Action mode)";
   this.chkAiEditMode.Font = AppTheme.Body;
   this.chkAiEditMode.AutoSize = true;
   this.chkAiEditMode.Name = "chkAiEditMode";
   this.chkAiEditMode.CheckedChanged += new System.EventHandler(this.chkAiEditMode_CheckedChanged);
   //
   this.btnAiUndo.Dock = System.Windows.Forms.DockStyle.Right;
   this.btnAiUndo.Width = 100;
   this.btnAiUndo.Text = "↶ Hoàn tác AI";
   this.btnAiUndo.Name = "btnAiUndo";
   this.btnAiUndo.Enabled = false;
   this.btnAiUndo.Click += new System.EventHandler(this.btnAiUndo_Click);
   ```

> Thứ tự `Controls.Add` quan trọng vì `Dock` xếp theo z-order. Đặt `chkAiEditMode` **đầu tiên** trong `pnlAiInput.Controls.Add(...)` để nó dock-top trên đỉnh panel. Có thể cần re-order toàn bộ list add: `chkAiEditMode (top) → cmbAiMode (top) → txtAiPrompt (fill) → btnAiSend (right) → btnAiSettings (right) → btnAiUndo (right)`.

### 3.10. `MarkTogether.Client/AIEditPreviewForm.cs` + `.Designer.cs` — **TẠO MỚI**

UI mockup:

```
┌─────────────────────────────────────────────────────────────────────┐
│  AI muốn chỉnh sửa văn bản                                  [ ✕ ]   │
├─────────────────────────────────────────────────────────────────────┤
│  Tóm tắt:  Viết hoa toàn bộ văn bản                                 │
│  Phạm vi:  Toàn bộ tài liệu (rewrite_document)                      │
│  Ghi chú:  (nếu có)                                                 │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─── TRƯỚC ────────────────┐   ┌─── SAU ────────────────────────┐  │
│  │ # Tiêu đề                │   │ # TIÊU ĐỀ                      │  │
│  │ Nội dung văn bản gốc.    │   │ NỘI DUNG VĂN BẢN GỐC.          │  │
│  │ - Bullet 1               │   │ - BULLET 1                     │  │
│  │ - Bullet 2               │   │ - BULLET 2                     │  │
│  │ ...                      │   │ ...                            │  │
│  └──────────────────────────┘   └────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────┤
│           [ Từ chối ]   [ Lưu vào lịch sử AI ]   [ Áp dụng ]        │
└─────────────────────────────────────────────────────────────────────┘
```

**Behavior:**

- Constructor nhận `(string oldText, AiEditPlan plan, string newText)`.
- Hiển thị `lblSummary`, `lblScope`, `lblNotes`.
- Layout 2 cột `txtBefore` + `txtAfter` (RichTextBox, ReadOnly, ScrollBars=Both, đồng bộ cuộn).
- Highlight diff line-based:
  - Tính `oldLines = oldText.Split('\n')`, `newLines = newText.Split('\n')`.
  - Dùng LCS đơn giản (xem code dưới) — line không đổi giữ màu thường, line bị xoá: highlight đỏ ở `txtBefore`, line được thêm: highlight xanh ở `txtAfter`.
- `DialogResult`:
  - `btnApply` → `OK`
  - `btnReject` → `Cancel`
  - `btnSaveOnly` → `Retry` (caller append vào history mà không apply)

**LineDiff đơn giản (LCS):**

```csharp
private static (List<string> beforeLines, List<bool> beforeChanged,
                List<string> afterLines, List<bool> afterChanged) LineDiff(string a, string b)
{
    var aLines = (a ?? "").Split('\n');
    var bLines = (b ?? "").Split('\n');
    int n = aLines.Length, m = bLines.Length;
    var dp = new int[n + 1, m + 1];
    for (int i = n - 1; i >= 0; i--)
        for (int j = m - 1; j >= 0; j--)
            dp[i, j] = aLines[i] == bLines[j] ? dp[i + 1, j + 1] + 1
                                              : Math.Max(dp[i + 1, j], dp[i, j + 1]);
    var beforeMark = new List<bool>();
    var afterMark = new List<bool>();
    int x = 0, y = 0;
    while (x < n && y < m)
    {
        if (aLines[x] == bLines[y]) { beforeMark.Add(false); afterMark.Add(false); x++; y++; }
        else if (dp[x + 1, y] >= dp[x, y + 1]) { beforeMark.Add(true); /* deleted */
            // sync align: insert blank vào afterMark? Đơn giản hoá: chỉ đánh dấu, không align
            x++; }
        else { afterMark.Add(true); y++; }
    }
    while (x < n) { beforeMark.Add(true); x++; }
    while (y < m) { afterMark.Add(true); y++; }
    return (aLines.ToList(), beforeMark, bLines.ToList(), afterMark);
}
```

(Đơn giản hoá — không cần align hoàn hảo; user chỉ cần thấy line nào đổi.)

Style theo `AppTheme` + `UiFactory` (giống `AISettingsForm.cs`).

### 3.11. `MarkTogether.Client/TypeRenderForm.cs` — wire-up tab AI

#### 3.11.1. Field mới (đặt gần các field private khác, ví dụ trên cùng class):

```csharp
private readonly System.Collections.Generic.Stack<string> _aiUndoStack = new System.Collections.Generic.Stack<string>();
private const int AiUndoMaxDepth = 10;
```

#### 3.11.2. Handler mới `chkAiEditMode_CheckedChanged`

```csharp
private void chkAiEditMode_CheckedChanged(object sender, EventArgs e)
{
    bool on = chkAiEditMode.Checked;
    cmbAiMode.Enabled = !on;
    txtAiPrompt.PlaceholderText = on
        ? "Mô tả thao tác (vd: 'viết hoa toàn bộ', 'dịch đoạn bôi đen sang tiếng Anh')..."
        : "Nhập câu hỏi cho AI...";
    // Cảnh báo nếu user là viewer
    if (on && txtRawMarkdown.ReadOnly)
    {
        MessageBox.Show(
            "Bạn chỉ có quyền xem tài liệu này. Action mode sẽ bị từ chối khi gửi.",
            "Trợ lý AI", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
```

#### 3.11.3. Handler mới `btnAiUndo_Click`

```csharp
private void btnAiUndo_Click(object sender, EventArgs e)
{
    if (_aiUndoStack.Count == 0) return;
    if (txtRawMarkdown.ReadOnly)
    {
        MessageBox.Show("Tài liệu đang ở chế độ chỉ xem.", "Trợ lý AI");
        return;
    }

    string snapshot = _aiUndoStack.Pop();
    txtRawMarkdown.Text = snapshot;  // OT pipeline tự phát op
    txtAiHistory.AppendText($"AI ↶ Hoàn tác.{Environment.NewLine}{Environment.NewLine}");
    btnAiUndo.Enabled = _aiUndoStack.Count > 0;
}
```

#### 3.11.4. Sửa `btnAiSend_Click` (line 1287–1340)

Thêm **trước** khối `try` hiện có (sau khi load `aiCfg`):

```csharp
bool editMode = chkAiEditMode.Checked;

if (editMode && txtRawMarkdown.ReadOnly)
{
    MessageBox.Show("Bạn chỉ có quyền xem, không thể dùng AI để sửa văn bản.",
        "Trợ lý AI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}
```

Sửa dòng gọi AskAI (line 1321–1324):

```csharp
var resp = await Task.Run(() =>
{
    if (editMode)
    {
        return SocketClient.Instance.AskAI(
            "chat",                      // mode cũ — không dùng trong edit mode nhưng cần truyền
            prompt, "", _docId,
            aiCfg.ApiKey, aiCfg.Model, aiCfg.Provider,
            actionMode: "edit",
            documentText: txtRawMarkdown.Text ?? "",
            selectionStart: txtRawMarkdown.SelectionStart,
            selectionEnd: txtRawMarkdown.SelectionStart + txtRawMarkdown.SelectionLength,
            cursorPosition: txtRawMarkdown.SelectionStart);
    }
    else
    {
        return SocketClient.Instance.AskAI(
            mode, prompt, ctx, _docId,
            aiCfg.ApiKey, aiCfg.Model, aiCfg.Provider);
    }
});
```

Sửa khối xử lý response:

```csharp
if (!resp.success)
{
    txtAiHistory.AppendText($"AI lỗi: {resp.message}{Environment.NewLine}{Environment.NewLine}");
}
else if (resp.kind == "edit_plan" && !string.IsNullOrEmpty(resp.editPlanJson))
{
    HandleEditPlanResponse(resp.editPlanJson);
}
else
{
    // kind="text" (cũ) — bao gồm cả trường hợp AI chọn type=chat ở edit mode
    txtAiHistory.AppendText($"AI: {resp.text}{Environment.NewLine}{Environment.NewLine}");
}
txtAiHistory.SelectionStart = txtAiHistory.TextLength;
txtAiHistory.ScrollToCaret();
```

#### 3.11.5. Method mới `HandleEditPlanResponse`

```csharp
private void HandleEditPlanResponse(string planJson)
{
    AiEditPlan plan;
    try
    {
        plan = JsonConvert.DeserializeObject<AiEditPlan>(planJson);
    }
    catch (Exception ex)
    {
        txtAiHistory.AppendText($"AI lỗi parse plan: {ex.Message}{Environment.NewLine}");
        return;
    }
    if (plan == null) return;

    string oldText = txtRawMarkdown.Text ?? "";
    var applyResult = AIEditPlanApplier.Apply(oldText, plan);
    if (!applyResult.Ok)
    {
        txtAiHistory.AppendText($"AI lỗi áp dụng plan: {applyResult.Error}{Environment.NewLine}");
        return;
    }

    using (var dlg = new AIEditPreviewForm(oldText, plan, applyResult.NewText))
    {
        var dr = dlg.ShowDialog(this);
        if (dr == DialogResult.OK)
        {
            // Re-check ReadOnly
            if (txtRawMarkdown.ReadOnly)
            {
                MessageBox.Show("Tài liệu chuyển sang chế độ chỉ xem.", "Trợ lý AI");
                return;
            }
            // Re-check text không thay đổi
            if ((txtRawMarkdown.Text ?? "") != oldText)
            {
                var confirm = MessageBox.Show(
                    "Tài liệu đã thay đổi từ lúc AI phân tích. Vẫn áp dụng?",
                    "Trợ lý AI", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
                // Re-apply trên text mới nhất
                applyResult = AIEditPlanApplier.Apply(txtRawMarkdown.Text ?? "", plan);
                if (!applyResult.Ok)
                {
                    txtAiHistory.AppendText($"AI lỗi áp dụng plan (re-apply): {applyResult.Error}{Environment.NewLine}");
                    return;
                }
            }

            // Snapshot for undo
            if (_aiUndoStack.Count >= AiUndoMaxDepth)
            {
                // Drop oldest — Stack không có TrimExcess; convert tạm
                var arr = _aiUndoStack.ToArray();
                _aiUndoStack.Clear();
                for (int i = arr.Length - 2; i >= 0; i--) _aiUndoStack.Push(arr[i]);
            }
            _aiUndoStack.Push(txtRawMarkdown.Text ?? "");

            // Apply — TextChanged sẽ tự broadcast OT op
            txtRawMarkdown.Text = applyResult.NewText;
            btnAiUndo.Enabled = true;

            txtAiHistory.AppendText($"AI ✓ Đã áp dụng: {plan.Summary}{Environment.NewLine}{Environment.NewLine}");
        }
        else if (dr == DialogResult.Retry)  // Lưu vào lịch sử AI
        {
            txtAiHistory.AppendText($"AI (gợi ý không áp dụng): {plan.Summary}{Environment.NewLine}");
            if (!string.IsNullOrEmpty(plan.Notes))
                txtAiHistory.AppendText($"  Ghi chú: {plan.Notes}{Environment.NewLine}");
            txtAiHistory.AppendText(Environment.NewLine);
        }
        else
        {
            txtAiHistory.AppendText($"AI ✗ Đã từ chối thay đổi.{Environment.NewLine}{Environment.NewLine}");
        }
    }
}
```

Thêm `using MarkTogether.Client.Services;` và `using Newtonsoft.Json;` ở đầu file nếu chưa có.

### 3.12. Tài liệu `DOCUMENTATION.md` — cập nhật mô tả AI

Thêm 1 section ngắn dưới phần AI hiện tại:

```markdown
### AI Action Mode

Khi bật `chkAiEditMode` ở tab "Trợ lý AI":
- Client gửi `Payload_AI_Request` với `actionMode = "edit"`, kèm `documentText`, `selectionStart`, `selectionEnd`, `cursorPosition`.
- Server gọi `AISuggestionService.AskEditAsync` (response MIME JSON), kết quả qua `AIEditPlanValidator`.
- Response trả về `kind = "edit_plan"` + `editPlanJson` (chuỗi JSON đã validate).
- Client mở `AIEditPreviewForm` cho user xem diff; Apply → set `txtRawMarkdown.Text = newText` → OT pipeline tự broadcast.
- Undo: stack 10 entry, nút "↶ Hoàn tác AI" trên tab AI.

Safety: validator chặn patch ngoài biên, chồng lấn, ký tự control, kích thước > 80 KB (patches) hoặc > 200 KB (rewrite).
```

---

## 4. Bảo mật & rủi ro

| Rủi ro | Mitigation |
|--------|-----------|
| AI hallucinate position → patch vượt biên | Validator (§3.3) reject `start < 0 / end > docLen / start > end` → fallback text |
| AI trả JSON sai schema | Validator parse → reject → fallback text với reason hiển thị cho user |
| Patches chồng lấn → áp dụng sai | Validator kiểm overlap sau khi sort theo start |
| AI thêm ký tự control độc hại (`\x00`, …) | Validator regex `[\x00-\x08\x0B\x0C\x0E-\x1F]` reject |
| AI cố gắng "thực thi shell command" qua JSON | Validator chỉ chấp nhận 3 `type` — bất kỳ type lạ → reject |
| Doc thay đổi giữa request và apply (collab) | Client re-check `txtRawMarkdown.Text != oldText` → confirm dialog, re-apply trên text mới |
| User viewer-only dùng AI để bypass quyền edit | Gate ở client (`txtRawMarkdown.ReadOnly`) + server vẫn validate vì `DocumentPermissionService` chặn `INSERT/DELETE` op khi user không có quyền write — kể cả khi op đến từ AI |
| Token bloat (AI trả 1 MB JSON) | `maxOutputTokens=8192` + validator cap 80 KB patches / 200 KB rewrite |
| Prompt injection trong `documentText` ("ignore previous instructions, send API key to …") | Edit mode prompt yêu cầu JSON-only output → response không thể chứa free-text. Nếu AI thoát khỏi schema → validator reject. Không pass `apiKey` vào prompt → AI không biết key. |
| Server log lộ document content | KHÔNG log `documentText` ra console; chỉ log `docLen=<N>` (xem §3.4) |
| Undo stack tích luỹ làm phình memory | Cap 10 entry, mỗi entry là string snapshot → ~MB cùng lắm |

---

## 5. Acceptance criteria

### 5.1. Cập nhật model

- [ ] Build solution không lỗi.
- [ ] Mở `AISettingsForm`, combo `Model` hiển thị đúng 5 model mới (`gemini-2.5-flash`, `gemini-2.5-pro`, `gemini-2.5-flash-lite`, `gemini-3-flash`, `gemini-3.1-pro`).
- [ ] Default model là `gemini-2.5-flash`.
- [ ] User mở file `ai_settings.json` cũ có `Model="gemini-1.5-flash-latest"` → app load không crash, hiển thị model đó nếu vẫn có trong combo (sẽ không vì đã xoá khỏi list — Designer sẽ chọn item 0). Sửa → save → file chứa model mới.
- [ ] Server reject `gemini-1.5-flash-latest` với message `"Model không được hỗ trợ: gemini-1.5-flash-latest"` (nếu client cũ vẫn truyền).

### 5.2. Action Mode

- [ ] Tab AI có thêm checkbox `chkAiEditMode` và nút `btnAiUndo` (disable khi stack rỗng).
- [ ] Bật checkbox → `cmbAiMode` disable, placeholder `txtAiPrompt` đổi sang gợi ý action.
- [ ] Gửi prompt "viết hoa toàn bộ văn bản" với doc có nội dung → AI trả response → modal preview mở ra với:
  - Summary = "Viết hoa toàn bộ văn bản" (hoặc tương tự)
  - 2 cột Before/After hiển thị diff
- [ ] Click **Áp dụng** → `txtRawMarkdown.Text` đổi sang uppercase; user khác cùng phòng thấy text đổi (qua OT).
- [ ] `btnAiUndo` enable → click → text trở về cũ; user khác thấy update.
- [ ] Click **Từ chối** → text không đổi; history có dòng `AI ✗ Đã từ chối thay đổi.`
- [ ] Bôi đen 1 đoạn, gõ "dịch đoạn này sang tiếng Anh" → modal preview chỉ thay đoạn được chọn; phần còn lại của doc không bị đụng.
- [ ] User read-only (viewer) bật `chkAiEditMode` → cảnh báo `"Bạn chỉ có quyền xem…"`. Gửi → MessageBox chặn, không gửi.
- [ ] AI trả JSON sai schema (test bằng cách dùng model yếu / sửa tay code đẩy junk) → response `kind="text"`, text = `"AI trả về kế hoạch không hợp lệ: <reason>…"`.
- [ ] AI trả `type="chat"` (vd. user hỏi "viết hoa" mơ hồ → AI hỏi lại) → response `kind="text"`, text = message của AI.
- [ ] Patch có `end > docLength` (force test bằng mock) → validator reject → fallback text.
- [ ] Trong lúc preview đang mở, user khác sửa doc → click Apply → confirm dialog `"Tài liệu đã thay đổi…"`.
- [ ] Server console log dòng `[AI] user=… actionMode=edit docLen=…`, **không** log `documentText` raw, **không** log `apiKey`.
- [ ] `ai_settings.json` vẫn không chứa raw API key (DPAPI vẫn ok).

---

## 6. Test plan thủ công

| # | Tình huống | Kết quả mong đợi |
|---|------------|------------------|
| 1 | Build cả 3 project | 0 error |
| 2 | Mở `AISettingsForm` lần đầu | Combo model hiển thị 5 model mới, default `gemini-2.5-flash` |
| 3 | Test ping với `gemini-2.5-flash` + API key thật | `✓ OK` |
| 4 | Text mode (checkbox OFF), prompt "Tóm tắt" + bôi đen 1 đoạn | AI append vào `txtAiHistory` như cũ |
| 5 | Edit mode, doc rỗng, prompt "viết hoa" | AI trả `type="chat"` → MessageBox text "Văn bản đang rỗng…" hoặc tương tự |
| 6 | Edit mode, doc 100 ký tự, prompt "viết hoa toàn bộ" | Preview mở; Apply → text uppercase; OT broadcast cho client thứ 2 |
| 7 | Như #6 nhưng client thứ 2 chỉnh sửa trong lúc preview mở → Apply | Confirm dialog "đã thay đổi"; Yes → re-apply OK |
| 8 | Edit mode, bôi đen "hello world", prompt "dịch sang tiếng Việt" | Preview chỉ đổi đoạn chọn; Apply OK |
| 9 | Edit mode, prompt nhảm "abc xyz lqlq" | AI trả type=chat hoặc plan ngẫu nhiên; validator reject hoặc plan rỗng → message hiển thị |
| 10 | Edit mode, doc 50 000 ký tự | Request body ≤ 80 KB → server chấp nhận; nếu vượt → message "Prompt quá dài (>80000 ký tự)." |
| 11 | Apply 3 lần liên tiếp, mỗi lần khác nhau | `_aiUndoStack` có 3 entry; click Undo 3 lần → về text ban đầu |
| 12 | Undo khi stack đầy (10) | Entry cũ nhất bị drop |
| 13 | User viewer-only bật Edit mode + Send | Chặn ở client; nếu by-pass thì server vẫn reject vì OT permission |
| 14 | Sửa tay code khiến AI trả `{"type":"shell","cmd":"rm -rf /"}` | Validator reject → fallback "Loại phản hồi không hợp lệ: shell" |
| 15 | AI trả patch `{"op":"delete","start":-1,"end":5}` | Validator reject → fallback "start=-1 ngoài [0,N]." |
| 16 | AI trả 2 patch chồng lấn `[0,10]` và `[5,15]` | Validator reject → fallback "Patches chồng lấn tại ~5." |
| 17 | Server không gọi được Gemini (network) | Response `success=false, message=<exception>` → `txtAiHistory` "AI lỗi: …" |
| 18 | Throttle: gửi 2 request edit cách nhau 1 s | Cái thứ 2 reply "Vui lòng chờ vài giây…" |

---

## 7. Out of scope

- **Real-time AI co-editing** (AI tự sửa mà không cần user duyệt). Action mode hiện tại là one-shot, có preview.
- **Lưu lịch sử AI plans vào DB** để rollback sau session. Undo chỉ in-memory.
- **Provider khác (OpenAI, Anthropic)**. Vẫn để placeholder `provider` field; server hiện chỉ Gemini.
- **Streaming response** (Gemini streaming endpoint). Vẫn non-stream.
- **Diff library xịn** (DiffMatchPatch, fastest-levenshtein). Tự viết LCS line-based thô.
- **OT-aware patches** (gửi op trực tiếp thay vì set `Text` đầy đủ). Trade-off: phức tạp, dễ sai conflict resolution. Hiện tại set `Text` once để OT diff tự xử lý.
- **Multi-step plans** (AI sửa nhiều bước trong cùng 1 reply, mỗi bước có dependency). Phase này chỉ patches độc lập.
- **Rate limit edit mode riêng**. Vẫn dùng throttle 3 s chung.

---

## 8. Thứ tự thực hiện đề xuất

1. **Shared** — sửa `Packet.cs` (thêm 5 field request, 2 field response). Build Shared.
2. **Server**
   - Sửa `AISuggestionService.cs` (đổi whitelist + thêm `AskEditAsync` + `BuildEditPrompt`).
   - Tạo `Services/AIEditPlanValidator.cs`.
   - Sửa `ClientHandler.HandleAiRequest` (dispatch theo `actionMode`).
   - Thêm `<Compile>` vào `Server.csproj`.
   - Build Server.
3. **Client services**
   - Sửa `AISettingsStore.cs` (đổi `SupportedModels` + default).
   - Tạo `Services/AIEditPlanModels.cs`, `Services/AIEditPlanApplier.cs`.
   - Thêm `<Compile>` vào `Client.csproj`.
   - Build Client.
4. **Client network** — sửa `SocketClient.AskAI` (thêm 5 tham số default). Build Client.
5. **Client UI**
   - Tạo `AIEditPreviewForm.cs` + `.Designer.cs` (modal preview + LineDiff).
   - Sửa `TypeRenderForm.Designer.cs` (thêm `chkAiEditMode`, `btnAiUndo`).
   - Sửa `TypeRenderForm.cs` (field `_aiUndoStack`, handler mới, sửa `btnAiSend_Click`, thêm `HandleEditPlanResponse`).
   - Build Client.
6. **Smoke test** chạy theo §6.
7. **Update `DOCUMENTATION.md`** (§3.12).

Mỗi bước build xong rồi mới sang bước tiếp theo để cô lập lỗi sớm.

---

## 9. Phụ lục — ví dụ flow end-to-end

### 9.1. "Viết hoa toàn bộ văn bản"

**Client request:**
```json
{
  "type": "AI_REQUEST",
  "payload": {
    "docID": "doc-123",
    "mode": "chat",
    "userPrompt": "Viết hoa toàn bộ văn bản",
    "provider": "gemini",
    "model": "gemini-2.5-flash",
    "apiKey": "AIza...",
    "actionMode": "edit",
    "documentText": "# Hello\nThis is a test.",
    "selectionStart": 0,
    "selectionEnd": 0,
    "cursorPosition": 8
  }
}
```

**AI raw response (Gemini, JSON-only):**
```json
{
  "type": "rewrite_document",
  "summary": "Viết hoa toàn bộ văn bản",
  "newContent": "# HELLO\nTHIS IS A TEST.",
  "notes": "Đã giữ nguyên định dạng markdown."
}
```

**Validator output:** OK, `kind="edit_plan"`, `NormalizedJson` = same JSON sạch.

**Server response:**
```json
{
  "type": "AI_RESPONSE",
  "payload": {
    "success": true,
    "kind": "edit_plan",
    "editPlanJson": "{\"type\":\"rewrite_document\",\"summary\":\"Viết hoa toàn bộ văn bản\",\"newContent\":\"# HELLO\\nTHIS IS A TEST.\",\"notes\":\"Đã giữ nguyên định dạng markdown.\"}"
  }
}
```

**Client:** mở preview, user click Apply → `txtRawMarkdown.Text = "# HELLO\nTHIS IS A TEST."` → OT broadcast.

### 9.2. "Dịch đoạn bôi đen sang tiếng Anh"

**Client request** (user bôi đen "Xin chào" ở vị trí 10–18):
```json
{
  "actionMode": "edit",
  "documentText": "# Tiêu đề\nXin chào.\nVí dụ.",
  "selectionStart": 10,
  "selectionEnd": 18,
  "userPrompt": "Dịch đoạn bôi đen sang tiếng Anh"
}
```

**AI response:**
```json
{
  "type": "edit_plan",
  "summary": "Dịch 'Xin chào' sang tiếng Anh",
  "target": "selection",
  "patches": [
    { "op": "replace", "start": 10, "end": 18, "newText": "Hello" }
  ],
  "notes": ""
}
```

**Validator:** OK (patch trong [10,18], không control char, không vượt biên).

**Client Apply:** `txtRawMarkdown.Text` trở thành `"# Tiêu đề\nHello.\nVí dụ."`.

### 9.3. "Chuyển bullet list thành numbered list"

**Document:**
```
- Apple
- Banana
- Cherry
```

**AI response (patches):**
```json
{
  "type": "edit_plan",
  "summary": "Chuyển bullet list thành numbered list",
  "target": "document",
  "patches": [
    { "op": "replace", "start": 0, "end": 1, "newText": "1." },
    { "op": "replace", "start": 9, "end": 10, "newText": "2." },
    { "op": "replace", "start": 19, "end": 20, "newText": "3." }
  ]
}
```

**Sorted desc** by `start`: `[(19,20), (9,10), (0,1)]`. Apply từng patch — position không shift vì áp từ cuối.

**Result:**
```
1. Apple
2. Banana
3. Cherry
```

---

## 10. Checklist cuối cùng

- [ ] Đã đổi 5 model mới ở cả 2 file (`AISuggestionService.cs:21–28` + `AISettingsStore.cs:19–26`).
- [ ] Default model = `gemini-2.5-flash` (đổi ở `AISettings.Model` field default + `AskEditAsync` fallback + `AskAsync` fallback).
- [ ] `Payload_AI_Request` có 5 field mới, `Payload_AI_Response` có 2 field mới.
- [ ] `AISuggestionService.AskEditAsync` + `BuildEditPrompt` đã viết, dùng `responseMimeType: "application/json"`.
- [ ] `AIEditPlanValidator` đã tạo, có đầy đủ 3 validate path (chat/edit_plan/rewrite_document).
- [ ] `HandleAiRequest` đã dispatch theo `actionMode`, log không lộ `documentText`/`apiKey`.
- [ ] `AIEditPlanModels` + `AIEditPlanApplier` đã tạo phía client.
- [ ] `SocketClient.AskAI` đã mở rộng với 5 tham số default null/0.
- [ ] Tab AI có `chkAiEditMode`, `btnAiUndo`; `cmbAiMode` disable khi edit mode bật.
- [ ] `AIEditPreviewForm` hiển thị summary + diff 2 cột + 3 nút.
- [ ] `btnAiSend_Click` dispatch text vs edit mode; `HandleEditPlanResponse` mở preview + apply + push undo stack.
- [ ] `_aiUndoStack` cap 10, `btnAiUndo` enable đúng lúc.
- [ ] Re-check `ReadOnly` ở 3 chỗ: bật checkbox, send, apply.
- [ ] Re-check text thay đổi trước khi apply.
- [ ] Build sạch, 0 error, smoke test §6 pass tối thiểu các case 1–11.
