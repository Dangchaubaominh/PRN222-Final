using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Document
{
    [Authorize]
    public class ViewDocModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IWebHostEnvironment _env;

        public ViewDocModel(IDocumentService documentService, IUserSubjectService userSubjectService, IWebHostEnvironment env)
        {
            _documentService = documentService;
            _userSubjectService = userSubjectService;
            _env = env;
        }

        public DocumentDto Document { get; set; } = default!;
        public string? FileContent { get; set; }
        public List<DocumentChunkDto> Chunks { get; set; } = new();
        public int TotalWords { get; set; }

        private bool CanAccess(Guid subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return _userSubjectService.IsAssigned(userId, subjectId);
        }

        public IActionResult OnGet(Guid id)
        {
            var document = _documentService.GetDocumentById(id);
            if (document == null) return NotFound("Tài liệu không tồn tại.");
            if (!CanAccess(document.SubjectId)) return Forbid();
            Document = document;

            var fileExtension = Path.GetExtension(document.FileName).ToLower();
            if (fileExtension == ".txt")
            {
                string physicalPath = Path.Combine(_env.WebRootPath, document.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(physicalPath))
                    FileContent = System.IO.File.ReadAllText(physicalPath);
            }

            // Load các chunk AI đã học từ tài liệu này
            Chunks = _documentService.GetChunksByDocumentId(id).ToList();
            TotalWords = Chunks.Sum(c => c.WordCount);

            return Page();
        }

        // Phục vụ nội dung file để xem trực tiếp (iframe PDF / thẻ img).
        // Không dùng thẳng "doc.FilePath" trong src vì tên file có thể chứa ký tự
        // đặc biệt (vd "#") khiến trình duyệt hiểu nhầm thành URL fragment và cắt cụt
        // đường dẫn trước khi gửi request — nên phải route qua id (Guid) an toàn.
        public IActionResult OnGetFile(Guid id)
        {
            var document = _documentService.GetDocumentById(id);
            if (document == null) return NotFound();
            if (!CanAccess(document.SubjectId)) return Forbid();

            string physicalPath = Path.Combine(_env.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(physicalPath)) return NotFound();

            string contentType = Path.GetExtension(document.FileName).ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            // Không truyền fileDownloadName để trình duyệt hiển thị inline thay vì ép tải về.
            return PhysicalFile(physicalPath, contentType);
        }
    }
}
