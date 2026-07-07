using System.Collections.Generic;
using RagChatbot.DAL.Entities;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        void Add(Notification notification);
        IEnumerable<Notification> GetByUser(int userId, int take);
        int CountUnread(int userId);
        void MarkAllRead(int userId);
        void Delete(int id, int userId);
        void DeleteAll(int userId);
    }
}
