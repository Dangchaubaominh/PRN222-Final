using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;

namespace RagChatbot.RazorPages.Pages.Subject
{
    [Authorize(Roles = "Admin, Lecturer")]
    public class EditModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IHubContext<SubjectHub> _subjectHub;

        public EditModel(ISubjectService subjectService, IHubContext<SubjectHub> subjectHub)
        {
            _subjectService = subjectService;
            _subjectHub = subjectHub;
        }

        [BindProperty]
        public SubjectDto Subject { get; set; } = new();

        public IActionResult OnGet(Guid id)
        {
            var subject = _subjectService.GetSubjectById(id);
            if (subject == null) return NotFound();
            Subject = subject;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                _subjectService.UpdateSubject(Subject);
                await _subjectHub.Clients.Group(SubjectHub.SubjectListGroup).SendAsync("SubjectListChanged");
                TempData["SuccessMessage"] = $"Đã cập nhật môn học \"{Subject.Name}\" thành công.";
                return RedirectToPage("Index");
            }
            return Page();
        }
    }
}
