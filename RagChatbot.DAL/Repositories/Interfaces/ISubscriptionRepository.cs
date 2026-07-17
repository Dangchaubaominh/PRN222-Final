using RagChatbot.DAL.Entities;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface ISubscriptionRepository
    {
        // Gói đang hiệu lực của người dùng (Active + chưa hết hạn); null nếu không có
        UserSubscription? GetActiveByUser(int userId);
        void Add(UserSubscription subscription);
        void Update(UserSubscription subscription);
    }
}
