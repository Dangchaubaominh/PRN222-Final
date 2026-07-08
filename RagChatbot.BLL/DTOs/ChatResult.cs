using System;
using System.Collections.Generic;

namespace RagChatbot.BLL.DTOs
{
    /// <summary>
    /// Kết quả một lượt hỏi chatbot: danh sách nguồn (tên tài liệu) và
    /// luồng câu trả lời (stream từng đoạn).
    /// </summary>
    public class ChatResult
    {
        public IReadOnlyList<SourceCitationDto> Sources { get; init; } = Array.Empty<SourceCitationDto>();
        public IAsyncEnumerable<string> Answer { get; init; } = default!;
    }
}
