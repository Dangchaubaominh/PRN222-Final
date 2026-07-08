using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Document
{
    [Authorize]
    public class DownloadModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IWebHostEnvironment _env;

        public DownloadModel(IDocumentService documentService, IUserSubjectService userSubjectService, IWebHostEnvironment env)
        {
            _documentService = documentService;
            _userSubjectService = userSubjectService;
            _env = env;
        }

        private bool CanAccess(Guid subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return _userSubjectService.IsAssigned(userId, subjectId);
        }

        public IActionResult OnGet(Guid id)
        {
            var document = _documentService.GetDocumentById(id);
            if (document == null) return NotFound("Không tìm thấy thông tin tài liệu.");
            if (!CanAccess(document.SubjectId)) return Forbid();

            string physicalPath = Path.Combine(_env.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(physicalPath))
                return NotFound("File gốc không còn tồn tại trên hệ thống.");

            return PhysicalFile(physicalPath, "application/octet-stream", document.FileName);
        }
    }
}
