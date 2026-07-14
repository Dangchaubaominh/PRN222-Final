using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RagChatbot.RazorPages.Pages.Document
{
    [Authorize]
    public class SearchModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IUserSubjectService _userSubjectService;

        public SearchModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IUserSubjectService userSubjectService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Query { get; set; }

        public SubjectDto? Subject { get; set; }

        public IEnumerable<DocumentChunkDto> Results { get; set; } = new List<DocumentChunkDto>();

        private bool CanAccess(Guid subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return _userSubjectService.IsAssigned(userId, subjectId);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Subject = _subjectService.GetSubjectById(SubjectId);
            if (Subject == null || !CanAccess(SubjectId))
                return Forbid();

            if (!string.IsNullOrWhiteSpace(Query))
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                string role = User.FindFirstValue(ClaimTypes.Role) ?? "";

                // Nhúng query + tìm kiếm semantic — toàn bộ qua BLL (không chạm DAL trực tiếp)
                Results = await _documentService.SearchChunksAsync(SubjectId, Query, userId, role, topK: 10);
            }

            return Page();
        }
    }
}
