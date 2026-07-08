using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Quiz
{
    [Authorize]
    public class TakeModel : PageModel
    {
        private readonly IQuizService _quizService;

        public TakeModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public QuizDto QuizDto { get; set; }

        [BindProperty]
        public Dictionary<int, string> Answers { get; set; } = new Dictionary<int, string>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            QuizDto = await _quizService.GetQuizByIdAsync(id);
            if (QuizDto == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _quizService.SubmitQuizAsync(id, userId, Answers);
            
            return RedirectToPage("Result", new { id = result.Id });
        }
    }
}
