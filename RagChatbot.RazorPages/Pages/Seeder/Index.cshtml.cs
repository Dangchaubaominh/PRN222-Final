using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Seeder
{
    /// <summary>
    /// Trang tạm thời — dùng một lần để seed tài liệu IT vào DB.
    /// Chỉ Admin mới truy cập được.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IDocumentProcessingService _processingService;
        private readonly IWebHostEnvironment _env;

        public IndexModel(
            ISubjectService subjectService,
            IDocumentService documentService,
            IDocumentProcessingService processingService,
            IWebHostEnvironment env)
        {
            _subjectService = subjectService;
            _documentService = documentService;
            _processingService = processingService;
            _env = env;
        }

        public List<SubjectDto> Subjects { get; set; } = new();
        public int DocCount { get; set; }

        public void OnGet()
        {
            Subjects = _subjectService.GetAllSubjects().ToList();
            DocCount = Subjects.Count * 3;
        }

        public async Task<IActionResult> OnPostRunAsync()
        {
            var subjects = _subjectService.GetAllSubjects().ToList();
            if (!subjects.Any())
            {
                TempData["ErrorMessage"] = "Không có môn học nào trong hệ thống. Hãy tạo môn học trước.";
                return RedirectToPage();
            }

            var templates = SeederTemplates.GetDocumentTemplates();
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var results = new List<string>();
            int templateIndex = 0;

            foreach (var subject in subjects)
            {
                int docsForThisSubject = 3;

                for (int i = 0; i < docsForThisSubject; i++)
                {
                    var (fileName, content) = templates[templateIndex % templates.Count];
                    templateIndex++;

                    // Tên file duy nhất để tránh trùng lặp
                    string uniqueFileName = $"{Path.GetFileNameWithoutExtension(fileName)}-{Guid.NewGuid().ToString("N")[..6]}.txt";

                    try
                    {
                        // 1. Upload tài liệu qua service
                        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                        int uploaderId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
                        var (uploadResult, _) = await _documentService.UploadDocumentAsync(
                            subject.Id, uniqueFileName, stream, uploadsFolder, uploaderId, 0 /* Public */);

                        if (uploadResult == DocumentUploadResult.Duplicate)
                        {
                            results.Add($"⚠️ [{subject.Code}] {uniqueFileName} — đã tồn tại, bỏ qua");
                            continue;
                        }
                        if (uploadResult == DocumentUploadResult.Error)
                        {
                            results.Add($"❌ [{subject.Code}] {uniqueFileName} — upload thất bại");
                            continue;
                        }
                        if (uploadResult == DocumentUploadResult.TooLarge || uploadResult == DocumentUploadResult.TooManyPages)
                        {
                            results.Add($"❌ [{subject.Code}] {uniqueFileName} — vượt giới hạn cho phép, bỏ qua");
                            continue;
                        }

                        // 2. Tìm document vừa tạo
                        var doc = _documentService.GetDocumentsBySubject(subject.Id, uploaderId, "Admin")
                                                  .OrderByDescending(d => d.UploadedAt)
                                                  .FirstOrDefault(d => d.FileName == uniqueFileName);

                        if (doc == null)
                        {
                            results.Add($"❌ [{subject.Code}] {uniqueFileName} — không tìm thấy sau upload");
                            continue;
                        }

                        // 3. Xử lý RAG (semantic chunk + embedding)
                        bool processed = await _processingService.ProcessDocumentAsync(doc.Id, _env.WebRootPath);
                        results.Add(processed
                            ? $"✅ [{subject.Code}] {uniqueFileName}"
                            : $"⚠️ [{subject.Code}] {uniqueFileName} — upload OK nhưng AI không đọc được");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"❌ [{subject.Code}] {uniqueFileName} — lỗi: {ex.Message}");
                    }
                }
            }

            TempData["SeedResults"] = results;
            return RedirectToPage("Result");
        }
    }
}
