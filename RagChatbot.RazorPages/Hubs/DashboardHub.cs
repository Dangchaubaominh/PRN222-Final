using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Hubs
{
    /// <summary>
    /// Hub cho dashboard sống:
    /// - Theo dõi presence (online) qua OnConnected/OnDisconnected, phát số online cho tất cả.
    /// - Broadcast: Admin gửi thông báo tới MỌI tài khoản — lưu DB (vào chuông) và
    ///   đẩy real-time cho người đang online; người offline sẽ thấy khi đăng nhập lại.
    /// </summary>
    [Authorize]
    public class DashboardHub : Hub
    {
        private readonly IPresenceTracker _presence;
        private readonly IRealtimeNotifier _notifier;
        private readonly IUserService _userService;

        public DashboardHub(
            IPresenceTracker presence,
            IRealtimeNotifier notifier,
            IUserService userService)
        {
            _presence = presence;
            _notifier = notifier;
            _userService = userService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null) _presence.Connect(userId);
            await Clients.All.SendAsync("OnlineChanged", _presence.OnlineCount);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (userId != null) _presence.Disconnect(userId);
            await Clients.All.SendAsync("OnlineChanged", _presence.OnlineCount);
            await base.OnDisconnectedAsync(exception);
        }

        // Chỉ Admin được gửi thông báo broadcast. Lưu vào chuông cho từng người
        // (bền vững) đồng thời đẩy real-time tới người đang online.
        public async Task Broadcast(string message)
        {
            if (Context.User?.IsInRole("Admin") != true) return;
            if (string.IsNullOrWhiteSpace(message)) return;

            string from = Context.User?.FindFirstValue(ClaimTypes.GivenName) ?? "Quản trị viên";
            var userIds = _userService.GetAllUsers().Select(u => u.Id).ToList();

            await _notifier.NotifyUsersAsync(userIds, $"📢 {from}: {message}", "info");
        }
    }
}
