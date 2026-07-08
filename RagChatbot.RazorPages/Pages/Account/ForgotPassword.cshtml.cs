using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public ForgotPasswordModel(IUserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        [BindProperty]
        public string Email { get; set; } = "";

        public bool Sent { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Luôn hiển thị thông báo giống nhau (bảo mật — không tiết lộ email có tồn tại không)
            Sent = true;

            string token = _userService.GeneratePasswordResetToken(Email);
            if (token != null)
            {
                // Tìm tên người dùng để cá nhân hoá email
                var users = _userService.GetAllUsers();
                string fullName = "";
                foreach (var u in users)
                    if (u.Email == Email) { fullName = u.FullName ?? u.Username; break; }

                string resetLink = Url.Page("/Account/ResetPassword",
                                            pageHandler: null,
                                            values: new { token },
                                            protocol: Request.Scheme);

                await _emailService.SendPasswordResetEmailAsync(Email, fullName, resetLink);
            }

            return Page();
        }
    }
}
