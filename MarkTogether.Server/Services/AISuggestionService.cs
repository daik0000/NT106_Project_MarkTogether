using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarkTogether.Server.Services
{
    /// <summary>
    /// Wrapper gọi Google Gemini.
    /// API key ưu tiên nhận từ client; App.config/GeminiApiKey chỉ là fallback dev/demo.
    /// </summary>
    public static class AISuggestionService
    {
        private const string BaseUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/";

        private static readonly HashSet<string> SupportedModels = new HashSet<string>
        {
            "gemini-2.5-flash",
            "gemini-2.5-pro",
            "gemini-2.5-flash-lite",
            "gemini-3-flash",
            "gemini-3.1-pro"
        };

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public static async Task<string> AskAsync(
            string provider, string model, string apiKey,
            string mode, string userPrompt, string contextText)
        {
            provider = string.IsNullOrWhiteSpace(provider)
                ? "gemini"
                : provider.Trim().ToLowerInvariant();
            if (provider != "gemini")
                throw new InvalidOperationException("Provider chưa được hỗ trợ: " + provider);

            model = string.IsNullOrWhiteSpace(model)
                ? "gemini-2.5-flash"
                : model.Trim();
            if (!SupportedModels.Contains(model))
                throw new InvalidOperationException("Model không được hỗ trợ: " + model);

            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Chưa cấu hình API key. Mở 'Cài đặt AI' để nhập.");

            string prompt = BuildPrompt(mode, userPrompt, contextText);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    maxOutputTokens = 1024
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
                    return string.IsNullOrEmpty(text) ? "(Không có câu trả lời)" : text.Trim();
                }
            }
        }

        public static async Task<string> AskEditAsync(
            string provider, string model, string apiKey,
            string userPrompt,
            string documentText, int documentLength,
            int selectionStart, int selectionEnd, int cursorPosition)
        {
            provider = string.IsNullOrWhiteSpace(provider)
                ? "gemini"
                : provider.Trim().ToLowerInvariant();
            if (provider != "gemini")
                throw new InvalidOperationException("Provider chưa được hỗ trợ: " + provider);

            model = string.IsNullOrWhiteSpace(model)
                ? "gemini-2.5-flash"
                : model.Trim();
            if (!SupportedModels.Contains(model))
                throw new InvalidOperationException("Model không được hỗ trợ: " + model);

            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Chưa cấu hình API key. Mở 'Cài đặt AI' để nhập.");

            string prompt = BuildEditPrompt(userPrompt, documentText, documentLength,
                selectionStart, selectionEnd, cursorPosition);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 8192,
                    responseMimeType = "application/json"
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

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength);
        }

        private static string BuildPrompt(string mode, string userPrompt, string contextText)
        {
            string ctx = string.IsNullOrWhiteSpace(contextText)
                ? ""
                : $"\n\n--- Đoạn văn bản ngữ cảnh ---\n{contextText}\n--- Hết ngữ cảnh ---\n";

            switch ((mode ?? "chat").ToLowerInvariant())
            {
                case "summarize":
                    return $"Tóm tắt ngắn gọn đoạn văn bản sau bằng tiếng Việt (3-5 gạch đầu dòng).{ctx}";
                case "continue":
                    return $"Viết tiếp đoạn văn bản markdown sau một cách tự nhiên, giữ nguyên giọng văn:{ctx}";
                case "translate":
                    string lang = string.IsNullOrWhiteSpace(userPrompt) ? "tiếng Anh" : userPrompt;
                    return $"Dịch đoạn văn bản sau sang {lang}, giữ định dạng markdown:{ctx}";
                case "chat":
                default:
                    return $"Bạn là trợ lý viết tài liệu markdown. Trả lời ngắn gọn, đúng trọng tâm.\n\nCâu hỏi: {userPrompt}{ctx}";
            }
        }

        private static string BuildEditPrompt(
            string userPrompt, string documentText, int documentLength,
            int selectionStart, int selectionEnd, int cursorPosition)
        {
            documentText = documentText ?? "";
            string truncatedDoc = documentText;
            if (truncatedDoc.Length > 40000)
                truncatedDoc = truncatedDoc.Substring(0, 40000) + "\n…(đã cắt bớt — tổng " + documentLength + " ký tự)";

            string selectedText = "";
            if (selectionStart >= 0 && selectionEnd > selectionStart && selectionEnd <= documentText.Length)
                selectedText = documentText.Substring(selectionStart, Math.Min(selectionEnd - selectionStart, 5000));

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
    }
}