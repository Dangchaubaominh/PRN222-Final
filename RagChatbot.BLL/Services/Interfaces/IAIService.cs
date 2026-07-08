using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IAIService
    {
        // Hàm này nhận vào 1 đoạn văn và trả về 1 mảng số (Vector)
        Task<float[]> GenerateEmbeddingAsync(string text);

        // Sinh câu trả lời theo kiểu streaming: trả về từng đoạn text khi AI tạo ra
        IAsyncEnumerable<string> GenerateChatResponseStreamAsync(string prompt, CancellationToken cancellationToken = default);

        // Sinh nội dung không streaming (dùng cho Quiz)
        Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
