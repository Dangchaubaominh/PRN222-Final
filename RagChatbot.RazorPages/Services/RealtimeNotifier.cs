using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;

namespace RagChatbot.RazorPages.Services
{
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly INotificationService _store;   // BLL: lưu trữ DB
        private readonly IHubContext<NotificationHub> _hub;

        public RealtimeNotifier(INotificationService store, IHubContext<NotificationHub> hub)
        {
            _store = store;
            _hub = hub;
        }

        public async Task NotifyUserAsync(int userId, string message, string type = "info", string? linkUrl = null)
        {
            var dto = _store.Create(userId, message, type, linkUrl);
            int unread = _store.GetUnreadCount(userId);
            await _hub.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                id = dto.Id,
                message = dto.Message,
                type = dto.Type,
                linkUrl = dto.LinkUrl,
                createdAt = dto.CreatedAt,
                unreadCount = unread
            });
        }

        public async Task NotifyUsersAsync(IEnumerable<int> userIds, string message, string type = "info", string? linkUrl = null)
        {
            foreach (var userId in userIds.Distinct())
                await NotifyUserAsync(userId, message, type, linkUrl);
        }

        public Task ForceLogoutAsync(int userId, string reason = "")
            => _hub.Clients.User(userId.ToString()).SendAsync("ForceLogout", new { reason });
    }
}
