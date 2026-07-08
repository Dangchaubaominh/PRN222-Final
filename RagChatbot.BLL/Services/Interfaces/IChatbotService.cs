using System;
using System.Threading;
using System.Threading.Tasks;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IChatbotService
    {
        // Hỏi chatbot: tìm ngữ cảnh (RAG) rồi trả về nguồn + luồng câu trả lời (streaming)
        Task<ChatResult> AskAsync(Guid subjectId, string userMessage, int currentUserId, string currentUserRole, CancellationToken cancellationToken = default);
    }
}
