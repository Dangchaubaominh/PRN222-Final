using RagChatbot.DAL.Entities;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface ISubscriptionService
    {
        // Gói đang hiệu lực của người dùng (null nếu không có)
        UserSubscription? GetActive(int userId);

        // TokenQuota - TokensUsed của gói hiện tại (0 nếu hết/không có gói)
        long GetRemainingQuota(int userId);

        // Cộng token đã dùng sau mỗi lần chat
        void AddUsedTokens(int userId, long tokens);

        // Kích hoạt/gia hạn gói sau khi thanh toán thành công (thay gói cũ, reset quota kỳ mới)
        void ActivateOrRenew(int userId, int packageId);
    }
}
