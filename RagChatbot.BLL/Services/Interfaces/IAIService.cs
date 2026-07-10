using RagChatbot.BLL.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IAIService
    {
        // Hàm này nhận vào 1 đoạn văn và trả về 1 mảng số (Vector)
        Task<float[]> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// Sinh câu trả lời streaming theo từng chunk text.
        /// Token usage được trả về qua tham số out <paramref name="usageRef"/> sau khi stream kết thúc.
        /// </summary>
        IAsyncEnumerable<string> GenerateChatResponseStreamAsync(
            string prompt,
            string model,
            Action<TokenUsage> onUsageAvailable,
            CancellationToken cancellationToken = default);

        /// <summary>Sinh nội dung không streaming (dùng cho Quiz). Trả về text + token đã dùng.</summary>
        Task<(string Content, TokenUsage Usage)> GenerateContentAsync(
            string prompt,
            string model,
            CancellationToken cancellationToken = default);
    }
}
