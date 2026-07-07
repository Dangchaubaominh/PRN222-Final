using System;
using System.Collections.Generic;
using System.Linq;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatMessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            _context.SaveChanges();
        }

        public ChatMessage? GetById(int id)
        {
            return _context.ChatMessages.FirstOrDefault(m => m.Id == id);
        }

        public void Update(ChatMessage message)
        {
            _context.ChatMessages.Update(message);
            _context.SaveChanges();
        }

        public IEnumerable<ChatMessage> GetHistory(int userId, Guid subjectId, int take)
        {
            // Lấy N tin gần nhất rồi đảo lại theo thứ tự thời gian tăng dần
            return _context.ChatMessages
                           .Where(m => m.UserId == userId && m.SubjectId == subjectId)
                           .OrderByDescending(m => m.Id)
                           .Take(take)
                           .OrderBy(m => m.Id)
                           .ToList();
        }
    }
}
