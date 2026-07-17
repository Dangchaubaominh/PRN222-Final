using System;
using System.Linq;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public UserSubscription? GetActiveByUser(int userId)
        {
            return _context.UserSubscriptions
                .Where(s => s.UserId == userId && s.Status == "Active" && s.ExpireAt > DateTime.UtcNow)
                .OrderByDescending(s => s.ExpireAt)
                .FirstOrDefault();
        }

        public void Add(UserSubscription subscription)
        {
            _context.UserSubscriptions.Add(subscription);
            _context.SaveChanges();
        }

        public void Update(UserSubscription subscription)
        {
            _context.UserSubscriptions.Update(subscription);
            _context.SaveChanges();
        }
    }
}
