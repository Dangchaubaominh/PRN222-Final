using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Quiz
{
    [Authorize]
    public class GenerateModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly IDocumentService _documentService;

        public GenerateModel(IQuizService quizService, IDocumentService documentService)
        {
            _quizService = quizService;
            _documentService = documentService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid DocumentId { get; set; }

        public string DocumentName { get; set; }

        [BindProperty]
        public int NumberOfQuestions { get; set; } = 10;

        public IActionResult OnGet()
        {
            var doc = _documentService.GetDocumentById(DocumentId);
            if (doc == null) return NotFound();

            DocumentName = doc.FileName;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (NumberOfQuestions < 5 || NumberOfQuestions > 20)
            {
                ModelState.AddModelError("", "Số lượng câu hỏi phải từ 5 đến 20.");
                return OnGet();
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var quiz = await _quizService.GenerateQuizAsync(DocumentId, NumberOfQuestions, userId);
                return RedirectToPage("Take", new { id = quiz.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi tạo Quiz: " + ex.Message);
                return OnGet();
            }
        }
    }
}
