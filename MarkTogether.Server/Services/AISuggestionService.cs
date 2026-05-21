using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarkTogether.Server.Services
{
    /// <summary>
    /// Wrapper gọi Google Gemini 1.5 Flash (free tier).
    /// API key đặt trong App.config: appSettings/GeminiApiKey
    /// </summary>
    public static class AISuggestionService
    {
        private const string Endpoint =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public static async Task<string> AskAsync(string mode, string userPrompt, string contextText)
        {
            string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Chưa cấu hình GeminiApiKey trong App.config.");

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
            string url = $"{Endpoint}?key={apiKey}";

            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                using (var res = await _http.SendAsync(req).ConfigureAwait(false))
                {
                    string body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!res.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            $"Gemini lỗi {(int)res.StatusCode}: {body}");
                    }

                    var jo = JObject.Parse(body);
                    string text = jo["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    return string.IsNullOrEmpty(text) ? "(Không có câu trả lời)" : text.Trim();
                }
            }
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
    }
}