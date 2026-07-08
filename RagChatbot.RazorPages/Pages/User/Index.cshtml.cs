using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Pages.User
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IDashboardNotifier _dashboard;
        private readonly IRealtimeNotifier _notifier;
        private readonly IHubContext<UserHub> _userHub;

        public IndexModel(IUserService userService, IDashboardNotifier dashboard, IRealtimeNotifier notifier, IHubContext<UserHub> userHub)
        {
            _userService = userService;
            _dashboard = dashboard;
            _notifier = notifier;
            _userHub = userHub;
        }

        public IEnumerable<UserManageDto> Users { get; set; } = new List<UserManageDto>();

        public void OnGet()
        {
            Users = _userService.GetAllUsers();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            string currentUsername = User.Identity!.Name!;
            bool success = _userService.DeleteUser(id, currentUsername);

            if (!success)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản này (bạn không thể tự xóa chính mình).";
            }
            else
            {
                // Tài khoản bị xóa → buộc đăng xuất nếu đang online
                await _notifier.ForceLogoutAsync(id, "deleted");
                await _dashboard.StatsChangedAsync();
                await _userHub.Clients.Group(UserHub.UserListGroup).SendAsync("UserListChanged", new
                {
                    action = "deleted",
                    userId = id
                });
            }

            return RedirectToPage();
        }
    }
}
