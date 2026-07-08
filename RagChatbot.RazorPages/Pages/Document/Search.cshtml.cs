using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Repositories.Interfaces;
using RagChatbot.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RagChatbot.RazorPages.Pages.Document
{
    [Authorize]
    public class SearchModel : PageModel
    {
        private readonly IAIService _aiService;
        private readonly IDocumentChunkRepository _chunkRepo;
        private readonly ISubjectService _subjectService;
        private readonly IUserSubjectService _userSubjectService;

        public SearchModel(
            IAIService aiService,
            IDocumentChunkRepository chunkRepo,
            ISubjectService subjectService,
            IUserSubjectService userSubjectService)
        {
            _aiService = aiService;
            _chunkRepo = chunkRepo;
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Query { get; set; }

        public SubjectDto? Subject { get; set; }
        
        public IEnumerable<DocumentChunk> Results { get; set; } = new List<DocumentChunk>();

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

                // Nhúng query thành vector
                float[] vector = await _aiService.GenerateEmbeddingAsync(Query);

                // Tìm kiếm vector
                Results = await _chunkRepo.SearchSimilarChunksAsync(SubjectId, vector, userId, role, topK: 10);
            }

            return Page();
        }
    }
}
