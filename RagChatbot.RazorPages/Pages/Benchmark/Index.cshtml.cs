using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.RazorPages.Pages.Benchmark
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;
        private readonly ISubjectService _subjectService;

        public IndexModel(IBenchmarkService benchmarkService, ISubjectService subjectService)
        {
            _benchmarkService = benchmarkService;
            _subjectService = subjectService;
        }

        public List<BenchmarkRun> Runs { get; set; } = new List<BenchmarkRun>();

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng chọn môn học")]
            public Guid SubjectId { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập ít nhất 1 câu hỏi")]
            public string QuestionsText { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 model")]
            public List<string> SelectedModels { get; set; }
        }

        public SelectList SubjectList { get; set; }
        
        public List<SelectListItem> ModelOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "gemini-2.5-flash-lite", Text = "Flash Lite (nhanh, rẻ)" },
            new SelectListItem { Value = "gemini-2.5-flash", Text = "Flash (cân bằng)" },
            new SelectListItem { Value = "gemini-2.5-pro", Text = "Pro (chậm, thông minh)" }
        };

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(adminIdStr, out int adminId))
            {
                return RedirectToPage("/Auth/Login");
            }

            var questionList = Input.QuestionsText
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .ToList();

            if (questionList.Count == 0 || Input.SelectedModels == null || Input.SelectedModels.Count == 0)
            {
                ModelState.AddModelError("", "Cần ít nhất 1 câu hỏi và 1 model.");
                await LoadDataAsync();
                return Page();
            }

            await _benchmarkService.RunAsync(adminId, Input.SubjectId, questionList, Input.SelectedModels);

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            Runs = await _benchmarkService.GetAllRunsAsync();
            var subjects = _subjectService.GetAllSubjects().ToList();
            SubjectList = new SelectList(subjects, "Id", "Name");
        }
    }
}
