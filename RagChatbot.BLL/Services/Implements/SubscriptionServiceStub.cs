using RagChatbot.BLL.Services.Interfaces;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Implements
{
    /// <summary>
    /// STUB TẠM — Luôn trả về quota cao để không chặn dev.
    /// Thay bằng SubscriptionService thật khi Bảo Minh push xong.
    /// </summary>
    public class SubscriptionServiceStub : ISubscriptionService
    {
        // Dev mode: không chặn — trả về quota "vô hạn"
        public long GetRemainingQuota(int userId) => long.MaxValue;

        // Không làm gì — subscription chưa có
        public void AddUsedTokens(int userId, long tokens) { }
    }
}
