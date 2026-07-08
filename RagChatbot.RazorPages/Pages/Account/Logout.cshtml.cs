using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RagChatbot.RazorPages.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync(bool forced = false)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (forced)
                TempData["SuccessMessage"] = "Tài khoản của bạn vừa được cập nhật quyền nên đã được đăng xuất. Vui lòng đăng nhập lại.";
            return RedirectToPage("/Account/Login");
        }
    }
}
