using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Quiz
{
    [Authorize]
    public class ResultModel : PageModel
    {
        private readonly IQuizService _quizService;

        public ResultModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public QuizResultDto Result { get; set; }
        public QuizDto QuizInfo { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Toàn bộ truy vấn dữ liệu đi qua BLL (không chạm DbContext trực tiếp)
            var detail = await _quizService.GetResultDetailAsync(id);
            if (detail == null) return NotFound();

            Result = detail.Value.Result;
            QuizInfo = detail.Value.Quiz;
            return Page();
        }
    }
}
