using System.Collections.Generic;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    /// <summary>
    /// Lưu trữ & truy vấn thông báo của người dùng (bền vững trong DB),
    /// để user offline vẫn xem được khi đăng nhập lại.
    /// </summary>
    public interface INotificationService
    {
        NotificationDto Create(int userId, string message, string type = "info", string? linkUrl = null);
        IEnumerable<NotificationDto> GetRecent(int userId, int take = 20);
        int GetUnreadCount(int userId);
        void MarkAllRead(int userId);
        void Delete(int id, int userId);
        void DeleteAll(int userId);
    }
}
