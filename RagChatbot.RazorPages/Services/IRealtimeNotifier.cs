namespace RagChatbot.RazorPages.Services
{
    /// <summary>
    /// Lưu thông báo vào DB (qua BLL) rồi đẩy real-time tới người dùng qua SignalR.
    /// Nhờ lưu DB nên user offline lúc đó vẫn thấy ở chuông khi đăng nhập lại.
    /// type: "success" | "info" | "warning" | "error".
    /// </summary>
    public interface IRealtimeNotifier
    {
        Task NotifyUserAsync(int userId, string message, string type = "info", string? linkUrl = null);
        Task NotifyUsersAsync(IEnumerable<int> userIds, string message, string type = "info", string? linkUrl = null);

        // Buộc một người dùng (nếu đang online) đăng xuất ngay lập tức
        Task ForceLogoutAsync(int userId, string reason = "");
    }
}
