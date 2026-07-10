using System;
using System.Threading;
using System.Threading.Tasks;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IChatbotService
    {
        /// <summary>
        /// Hỏi chatbot: tìm ngữ cảnh (RAG) rồi trả về nguồn + luồng câu trả lời (streaming).
        /// <paramref name="model"/> là model Gemini user đã chọn (theo gói của họ).
        /// </summary>
        Task<ChatResult> AskAsync(
            Guid subjectId,
            string userMessage,
            int currentUserId,
            string currentUserRole,
            string model,
            CancellationToken cancellationToken = default);
    }
}

