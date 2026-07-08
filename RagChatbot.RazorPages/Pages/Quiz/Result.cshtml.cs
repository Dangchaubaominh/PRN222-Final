using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace RagChatbot.RazorPages.Pages.Quiz
{
    [Authorize]
    public class ResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public QuizResultDto Result { get; set; }
        public QuizDto QuizInfo { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var qr = await _context.QuizResults
                .Include(r => r.Quiz)
                .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (qr == null) return NotFound();

            Result = new QuizResultDto
            {
                Id = qr.Id,
                QuizId = qr.QuizId,
                UserId = qr.UserId,
                Score = qr.Score,
                TotalQuestions = qr.TotalQuestions,
                CompletedAt = qr.CompletedAt
            };

            QuizInfo = new QuizDto
            {
                Title = qr.Quiz.Title,
                Questions = qr.Quiz.Questions.Select(q => new QuizQuestionDto
                {
                    Content = q.Content,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption,
                    Explanation = q.Explanation
                }).ToList()
            };

            return Page();
        }
    }
}
