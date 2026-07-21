using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;
using RagChatbot.RazorPages.BackgroundTasks;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Pages.Document
{
    [Authorize(Roles = "Admin, Lecturer")]
    public class CreateModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IWebHostEnvironment _env;
        private readonly IDocumentProcessingQueue _processingQueue;
        private readonly IDashboardNotifier _dashboard;
        private readonly IHubContext<DocumentHub> _documentHub;

        public CreateModel(
            IDocumentService documentService,
            IUserSubjectService userSubjectService,
            IWebHostEnvironment env,
            IDocumentProcessingQueue processingQueue,
            IDashboardNotifier dashboard,
            IHubContext<DocumentHub> documentHub)
        {
            _documentService = documentService;
            _userSubjectService = userSubjectService;
            _env = env;
            _processingQueue = processingQueue;
            _dashboard = dashboard;
            _documentHub = documentHub;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        [BindProperty]
        public int AccessLevel { get; set; }

        private bool CanAccess(Guid subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return _userSubjectService.IsAssigned(userId, subjectId);
        }

        public IActionResult OnGet()
        {
            if (!CanAccess(SubjectId)) return Forbid();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!CanAccess(SubjectId)) return Forbid();

            if (UploadFile == null || UploadFile.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn một file hợp lệ để tải lên.");
                return Page();
            }

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");

            using (var stream = UploadFile.OpenReadStream())
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var (result, pdfPageCount) = await _documentService.UploadDocumentAsync(SubjectId, UploadFile.FileName, stream, uploadsFolder, userId, AccessLevel);

                switch (result)
                {
                    case DocumentUploadResult.Duplicate:
                        ModelState.AddModelError("", $"Tài liệu \"{UploadFile.FileName}\" đã tồn tại trong môn học này. Vui lòng đổi tên file hoặc xóa tài liệu cũ trước khi upload lại.");
                        return Page();

                    case DocumentUploadResult.TooLarge:
                        ModelState.AddModelError("", $"File \"{UploadFile.FileName}\" quá lớn ({UploadFile.Length / 1024.0 / 1024.0:F1}MB). Kích thước tối đa cho phép là 20MB.");
                        return Page();

                    case DocumentUploadResult.TooManyPages:
                        ModelState.AddModelError("", $"File \"{UploadFile.FileName}\" có {pdfPageCount} trang, vượt quá giới hạn tối đa 300 trang.");
                        return Page();

                    case DocumentUploadResult.Error:
                        ModelState.AddModelError("", "Có lỗi xảy ra khi lưu file. Vui lòng thử lại.");
                        return Page();

                    case DocumentUploadResult.Success:
                        int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                        string role = User.FindFirstValue(ClaimTypes.Role) ?? "";
                        var uploadedDoc = _documentService.GetDocumentsBySubject(SubjectId, currentUserId, role)
                                                          .FirstOrDefault(d => d.FileName == UploadFile.FileName);
                        if (uploadedDoc != null)
                        {
                            // Đẩy vào hàng đợi nền — worker sẽ xử lý RAG và báo trạng thái
                            // real-time qua SignalR, trang không phải chờ.
                            _processingQueue.Enqueue(uploadedDoc.Id);
                            await _documentHub.Clients.Group(DocumentHub.GroupName(SubjectId)).SendAsync("DocumentListChanged", new
                            {
                                action = "created",
                                documentId = uploadedDoc.Id,
                                subjectId = SubjectId
                            });
                        }
                        await _dashboard.StatsChangedAsync();
                        TempData["SuccessMessage"] = "Upload thành công! AI đang xử lý tài liệu ở chế độ nền — trạng thái sẽ tự cập nhật.";
                        return RedirectToPage("Index", new { subjectId = SubjectId });
                }
            }

            return RedirectToPage("Index", new { subjectId = SubjectId });
        }
    }
}
