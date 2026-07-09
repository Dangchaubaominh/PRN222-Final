using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.ViewComponents
{
    /// <summary>
    /// Render chuông thông báo trên topbar: số chưa đọc + danh sách gần đây.
    /// Dữ liệu lấy từ DB (BLL) nên user offline lúc đó vẫn thấy khi đăng nhập lại.
    /// </summary>
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _store;

        public NotificationBellViewComponent(INotificationService store)
        {
            _store = store;
        }

        public IViewComponentResult Invoke()
        {
            var vm = new NotificationBellViewModel();

            var idValue = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idValue, out int userId))
            {
                vm.UnreadCount = _store.GetUnreadCount(userId);
                vm.Items = _store.GetRecent(userId, 20).ToList();
            }

            return View(vm);
        }
    }

    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Items { get; set; } = new();
    }
}
