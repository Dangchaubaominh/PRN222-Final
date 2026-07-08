using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;
using RagChatbot.RazorPages.Services;
using System.Security.Claims;

namespace RagChatbot.RazorPages.Pages.Subject
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IHubContext<SubjectHub> _subjectHub;
        private readonly IDashboardNotifier _dashboard;

        public IndexModel(
            ISubjectService subjectService,
            IUserSubjectService userSubjectService,
            IHubContext<SubjectHub> subjectHub,
            IDashboardNotifier dashboard)
        {
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
            _subjectHub = subjectHub;
            _dashboard = dashboard;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        // Lấy môn học theo vai trò (Admin: tất cả; còn lại: môn được gán)
        private void LoadSubjects()
        {
            if (User.IsInRole("Admin"))
            {
                Subjects = _subjectService.GetAllSubjects();
                return;
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            Subjects = _userSubjectService.GetAssignedSubjects(userId);
        }

        public void OnGet() => LoadSubjects();

        // Trả về phần card (gọi qua AJAX khi nhận SubjectListChanged)
        public IActionResult OnGetCardsPartial()
        {
            LoadSubjects();
            return Partial("_SubjectCards", this);
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var subject = _subjectService.GetSubjectById(id);
            string subjectName = subject?.Name ?? "Môn học";
            _subjectService.DeleteSubject(id);

            // Cập nhật danh sách môn học cho mọi người đang xem + dashboard
            await _subjectHub.Clients.Group(SubjectHub.SubjectListGroup).SendAsync("SubjectListChanged");
            await _dashboard.StatsChangedAsync();

            TempData["SuccessMessage"] = $"Đã xóa môn học \"{subjectName}\" thành công.";
            return RedirectToPage();
        }
    }
}
