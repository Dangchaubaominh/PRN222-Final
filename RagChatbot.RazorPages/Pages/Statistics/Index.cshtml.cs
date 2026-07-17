using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Statistics
{
    [Authorize(Roles = "Admin,Lecturer")]
    public class IndexModel : PageModel
    {
        private readonly IStatisticsService _stats;

        public IndexModel(IStatisticsService stats)
        {
            _stats = stats;
        }

        public bool IsAdmin { get; set; }
        public AdminStats? Admin { get; set; }
        public LecturerStats? Lecturer { get; set; }

        public void OnGet()
        {
            IsAdmin = User.IsInRole("Admin");
            if (IsAdmin)
            {
                Admin = _stats.GetAdminStats(30);
            }
            else
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                Lecturer = _stats.GetLecturerStats(userId);
            }
        }

        // Xuất CSV doanh thu + token (chỉ Admin)
        public IActionResult OnGetExportCsv()
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var a = _stats.GetAdminStats(30);
            var sb = new StringBuilder();
            sb.AppendLine("Ngay,DoanhThu(VND)");
            foreach (var d in a.RevenueByDay) sb.AppendLine($"{d.Label},{d.Amount}");
            sb.AppendLine();
            sb.AppendLine("Model,Token");
            foreach (var m in a.TokensByModel) sb.AppendLine($"{m.Model},{m.Tokens}");

            // BOM để Excel đọc đúng UTF-8 tiếng Việt
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", "thong-ke.csv");
        }
    }
}
