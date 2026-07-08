using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Pages.Document
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IWebHostEnvironment _env;
        private readonly IDashboardNotifier _dashboard;
        private readonly IHubContext<DocumentHub> _documentHub;

        public IndexModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IUserSubjectService userSubjectService,
            IWebHostEnvironment env,
            IDashboardNotifier dashboard,
            IHubContext<DocumentHub> documentHub)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
            _env = env;
            _dashboard = dashboard;
            _documentHub = documentHub;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        public SubjectDto? Subject { get; set; }
        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();

        // Admin xem mọi môn; còn lại chỉ môn mình là thành viên
        private bool CanAccess(Guid subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return _userSubjectService.IsAssigned(userId, subjectId);
        }

        public IActionResult OnGet()
        {
            Subject = _subjectService.GetSubjectById(SubjectId);
            if (Subject == null || !CanAccess(SubjectId))
                return Forbid();

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            string role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            Documents = _documentService.GetDocumentsBySubject(SubjectId, userId, role);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid subjectId)
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Lecturer"))) return Forbid();
            if (!CanAccess(subjectId)) return Forbid();

            var doc = _documentService.GetDocumentById(id);
            string fileName = doc?.FileName ?? "Tài liệu";
            _documentService.DeleteDocument(id, _env.WebRootPath);
            await _dashboard.StatsChangedAsync();
            await _documentHub.Clients.Group(DocumentHub.GroupName(subjectId)).SendAsync("DocumentListChanged", new
            {
                action = "deleted",
                documentId = id,
                subjectId
            });
            TempData["SuccessMessage"] = $"Đã xóa tài liệu \"{fileName}\" thành công.";
            return RedirectToPage(new { subjectId });
        }
    }
}
