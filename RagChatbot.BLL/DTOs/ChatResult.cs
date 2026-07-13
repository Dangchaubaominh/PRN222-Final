using System;
using System.Collections.Generic;

namespace RagChatbot.BLL.DTOs
{
    /// <summary>
    /// Thông tin token tiêu thụ trả về từ Gemini API (usageMetadata).
    /// </summary>
    public record TokenUsage(int PromptTokens, int CompletionTokens, int TotalTokens);

    /// <summary>
    /// Kết quả một lượt hỏi chatbot: danh sách nguồn (tên tài liệu),
    /// luồng câu trả lời (stream từng đoạn), và thông tin token đã dùng.
    /// </summary>
    public class ChatResult
    {
        public IReadOnlyList<SourceCitationDto> Sources { get; init; } = Array.Empty<SourceCitationDto>();
        public IAsyncEnumerable<string> Answer { get; set; } = default!;

        /// <summary>
        /// Token tiêu thụ — được gán sau khi stream hoàn tất (lấy từ usageMetadata Gemini).
        /// ChatHub dùng giá trị này để gọi TokenUsageService.Log() và AddUsedTokens().
        /// </summary>
        public TokenUsage? Usage { get; set; }
    }
}
