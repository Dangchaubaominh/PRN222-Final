using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;
using System.Security.Claims;

namespace RagChatbot.RazorPages.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;

        public LoginModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public string? Error { get; set; }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Home/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userDto = _userService.Authenticate(Username, Password);
            if (userDto != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userDto.Id.ToString()),
                    new Claim(ClaimTypes.Name,      userDto.Username),
                    new Claim(ClaimTypes.GivenName, userDto.FullName ?? userDto.Username),
                    new Claim(ClaimTypes.Role,      userDto.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                             new ClaimsPrincipal(identity));
                return RedirectToPage("/Home/Index");
            }

            Error = "Tài khoản hoặc mật khẩu không chính xác!";
            return Page();
        }
    }
}
