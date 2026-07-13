using Microsoft.Extensions.Configuration;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Implements
{
    public class GeminiService : IAIService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiKey;

        // Model mặc định khi caller không chỉ định (gói Free)
        private const string DefaultModel = "gemini-2.5-flash-lite";

        public GeminiService(IConfiguration config)
        {
            _apiKey = config["Gemini:ApiKey"];
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            string url = $"https://generativelanguage.googleapis.com/v1/models/gemini-embedding-001:embedContent?key={_apiKey}";

            var requestBody = new
            {
                model = "models/gemini-embedding-001",
                content = new
                {
                    parts = new[] { new { text = text } }
                },
                // Ép Google trả về đúng 768 chiều để khớp với cấu hình PostgreSQL
                outputDimensionality = 768
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                string errorDetail = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi từ Gemini API (HTTP {(int)response.StatusCode}): {errorDetail}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseString);

            var values = jsonDoc.RootElement
                .GetProperty("embedding")
                .GetProperty("values")
                .EnumerateArray();

            var vectorList = new List<float>();
            foreach (var value in values)
                vectorList.Add(value.GetSingle());

            return vectorList.ToArray();
        }

        /// <summary>
        /// Streaming chat — yield từng chunk text.
        /// Sau chunk cuối, đọc usageMetadata và gọi <paramref name="onUsageAvailable"/> callback.
        /// </summary>
        public async IAsyncEnumerable<string> GenerateChatResponseStreamAsync(
            string prompt,
            string model,
            Action<TokenUsage> onUsageAvailable,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Dùng model do caller truyền vào (theo gói của user)
            string safeModel = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{safeModel}:streamGenerateContent?alt=sse&key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = BuildFullPrompt(prompt) } } }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            // ResponseHeadersRead: bắt đầu đọc ngay khi có header, không đợi tải hết
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorDetail = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Lỗi Gemini Stream API: {errorDetail}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            TokenUsage? lastUsage = null;
            string? line;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                // SSE: mỗi sự kiện là dòng "data: {json}", phân tách bởi dòng trống
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                string json = line.Substring("data:".Length).Trim();
                if (json == "[DONE]")
                    break;

                // Bóc text chunk
                string? delta = ExtractText(json);
                if (!string.IsNullOrEmpty(delta))
                    yield return delta;

                // Đọc usageMetadata nếu có (thường xuất hiện ở chunk cuối cùng)
                TokenUsage? usage = ExtractUsage(json);
                if (usage != null)
                    lastUsage = usage;
            }

            // Sau khi stream xong — báo token count cho ChatHub qua callback
            onUsageAvailable(lastUsage ?? new TokenUsage(0, 0, 0));
        }

        /// <summary>Sinh nội dung không streaming (dùng cho Quiz). Trả về text + token đã dùng.</summary>
        public async Task<(string Content, TokenUsage Usage)> GenerateContentAsync(
            string prompt,
            string model,
            CancellationToken cancellationToken = default)
        {
            string safeModel = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{safeModel}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorDetail = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Lỗi Gemini API: {errorDetail}");
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseString);

            string content = null;
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                content = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }

            TokenUsage usage = ExtractUsageFromRoot(doc.RootElement) ?? new TokenUsage(0, 0, 0);
            return (content, usage);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        // Gắn System Prompt thẳng vào câu hỏi để ép AI không được "bịa" thông tin
        private static string BuildFullPrompt(string prompt) => @"
            Bạn là trợ lý học tập AI.

            Trước khi trả lời, hãy xác định loại câu hỏi:

            - Nếu là câu giao tiếp thông thường (xin chào, cảm ơn, tạm biệt, giới thiệu bản thân, hỏi ngày tháng, thời gian, thời tiết...) thì trả lời bình thường.

            - Nếu là câu hỏi học tập hoặc liên quan đến nội dung môn học thì chỉ sử dụng thông tin trong phần NGỮ CẢNH bên dưới.

            Đối với câu hỏi học tập:
            - Không được sử dụng kiến thức bên ngoài.
            - Không được suy đoán.
            - Không được bịa thông tin.
            - Nếu ngữ cảnh không chứa câu trả lời thì trả lời:
              'Xin lỗi, tôi không tìm thấy thông tin này trong tài liệu môn học.'

            NGỮ CẢNH:
            " + prompt;

        /// <summary>Bóc text từ 1 chunk JSON của Gemini SSE; trả null nếu không có/không hợp lệ</summary>
        private static string? ExtractText(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    return null;

                return candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch { return null; }
        }

        /// <summary>Bóc usageMetadata từ một chunk SSE (thường xuất hiện ở chunk cuối)</summary>
        private static TokenUsage? ExtractUsage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return ExtractUsageFromRoot(doc.RootElement);
            }
            catch { return null; }
        }

        /// <summary>Đọc promptTokenCount / candidatesTokenCount / totalTokenCount từ usageMetadata</summary>
        private static TokenUsage? ExtractUsageFromRoot(JsonElement root)
        {
            if (!root.TryGetProperty("usageMetadata", out var meta))
                return null;

            int promptTokens      = meta.TryGetProperty("promptTokenCount",     out var p) ? p.GetInt32() : 0;
            int completionTokens  = meta.TryGetProperty("candidatesTokenCount", out var c) ? c.GetInt32() : 0;
            int totalTokens       = meta.TryGetProperty("totalTokenCount",      out var t) ? t.GetInt32() : promptTokens + completionTokens;

            return new TokenUsage(promptTokens, completionTokens, totalTokens);
        }
    }
}
