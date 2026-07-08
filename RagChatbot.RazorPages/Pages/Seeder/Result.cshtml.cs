using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RagChatbot.RazorPages.Pages.Seeder
{
    [Authorize(Roles = "Admin")]
    public class ResultModel : PageModel
    {
        public List<string> Results { get; set; } = new();

        public void OnGet()
        {
            Results = TempData["SeedResults"] as List<string> ?? new List<string>();
        }
    }
}
