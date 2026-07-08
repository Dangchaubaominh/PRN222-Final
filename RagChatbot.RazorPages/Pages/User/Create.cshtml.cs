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
    public class CreateModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IDashboardNotifier _dashboard;
        private readonly IHubContext<UserHub> _userHub;

        public CreateModel(IUserService userService, IEmailService emailService, IDashboardNotifier dashboard, IHubContext<UserHub> userHub)
        {
            _userService = userService;
            _emailService = emailService;
            _dashboard = dashboard;
            _userHub = userHub;
        }

        [BindProperty]
        public UserManageDto Input { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (string.IsNullOrWhiteSpace(Input.Password))
            {
                ModelState.AddModelError("Input.Password", "Mật khẩu không được để trống");
                return Page();
            }

            // Lưu tài khoản vào DB
            _userService.CreateUser(Input);
            await _dashboard.StatsChangedAsync();
            await _userHub.Clients.Group(UserHub.UserListGroup).SendAsync("UserListChanged", new
            {
                action = "created",
                username = Input.Username
            });

            // Lấy thông tin admin đang đăng nhập để bind vào email
            string adminUsername = User.Identity!.Name!;
            var adminInfo = _userService.GetByUsername(adminUsername);
            string adminName = adminInfo?.FullName ?? adminUsername;
            string adminEmail = adminInfo?.Email ?? "";

            // Gửi email thông tin tài khoản (bắt buộc vì Email là required)
            string displayName = Input.FullName ?? Input.Username;
            bool sent = await _emailService.SendAccountCredentialsAsync(
                Input.Email, displayName, Input.Username, Input.Password,
                adminName, adminEmail);

            TempData["SuccessMessage"] = sent
                ? $"Đã tạo tài khoản và gửi thông tin đến {Input.Email} thành công."
                : $"Đã tạo tài khoản nhưng không thể gửi email đến {Input.Email}. Kiểm tra cấu hình SMTP.";

            return RedirectToPage("Index");
        }
    }
}
