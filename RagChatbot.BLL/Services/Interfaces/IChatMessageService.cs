using System;
using System.Collections.Generic;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IChatMessageService
    {
        int Save(int userId, Guid subjectId, string sender, string content, IReadOnlyList<SourceCitationDto>? sources = null);
        void UpdateFeedback(int messageId, int userId, RagChatbot.DAL.Entities.FeedbackType? feedback);
        IEnumerable<ChatMessageDto> GetHistory(int userId, Guid subjectId, int take = 50);
    }
}
