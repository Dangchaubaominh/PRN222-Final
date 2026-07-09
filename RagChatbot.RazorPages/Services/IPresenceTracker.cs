namespace RagChatbot.RazorPages.Services
{
    /// <summary>
    /// Theo dõi số người dùng đang online (đếm theo user, không đếm theo tab).
    /// </summary>
    public interface IPresenceTracker
    {
        // true nếu user vừa chuyển từ offline → online (kết nối đầu tiên)
        bool Connect(string userId);
        // true nếu user vừa chuyển từ online → offline (đóng kết nối cuối)
        bool Disconnect(string userId);
        int OnlineCount { get; }
    }
}
