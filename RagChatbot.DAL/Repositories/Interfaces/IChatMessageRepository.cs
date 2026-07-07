using System;
using System.Collections.Generic;
using RagChatbot.DAL.Entities;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface IChatMessageRepository
    {
        void Add(ChatMessage message);
        ChatMessage? GetById(int id);
        void Update(ChatMessage message);
        // Lịch sử hội thoại của 1 user trong 1 môn học, cũ → mới
        IEnumerable<ChatMessage> GetHistory(int userId, Guid subjectId, int take);
    }
}
