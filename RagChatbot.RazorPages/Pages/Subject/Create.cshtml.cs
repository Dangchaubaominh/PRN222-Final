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
    [Authorize(Roles = "Admin, Lecturer")]
    public class CreateModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IHubContext<SubjectHub> _subjectHub;
        private readonly IDashboardNotifier _dashboard;

        public CreateModel(
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

        [BindProperty]
        public SubjectDto Subject { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            _subjectService.CreateSubject(Subject);

            if (User.IsInRole("Lecturer"))
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _userSubjectService.Assign(userId, Subject.Id);
            }

            await _subjectHub.Clients.Group(SubjectHub.SubjectListGroup).SendAsync("SubjectListChanged");
            await _dashboard.StatsChangedAsync();

            TempData["SuccessMessage"] = $"Đã tạo môn học \"{Subject.Name}\" thành công.";
            return RedirectToPage("Index");
        }
    }
}
