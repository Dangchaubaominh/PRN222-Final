using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly IUserService _userService;

        public ResetPasswordModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public string Token { get; set; } = "";

        [BindProperty]
        public string NewPassword { get; set; } = "";

        [BindProperty]
        public string ConfirmPassword { get; set; } = "";

        public bool Expired { get; set; }
        public string? Error { get; set; }

        public IActionResult OnGet(string token)
        {
            if (!_userService.IsValidResetToken(token))
            {
                Expired = true;
                return Page();
            }

            Token = token;
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!_userService.IsValidResetToken(Token))
            {
                Expired = true;
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                Error = "Mật khẩu xác nhận không khớp.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 4)
            {
                Error = "Mật khẩu phải có ít nhất 4 ký tự.";
                return Page();
            }

            _userService.ResetPassword(Token, NewPassword);

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Hãy đăng nhập bằng mật khẩu mới.";
            return RedirectToPage("/Account/Login");
        }
    }
}
