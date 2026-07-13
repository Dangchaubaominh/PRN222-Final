using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace RagChatbot.RazorPages.Pages.Subject
{
    [Authorize(Roles = "Admin")]
    public class ChunkConfigModel : PageModel
    {
        private readonly IChunkConfigService _chunkConfigService;
        private readonly ISubjectService _subjectService;

        public ChunkConfigModel(IChunkConfigService chunkConfigService, ISubjectService subjectService)
        {
            _chunkConfigService = chunkConfigService;
            _subjectService = subjectService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        public string SubjectName { get; set; }

        [BindProperty]
        public ChunkConfigViewModel Input { get; set; }

        public class ChunkConfigViewModel
        {
            [Required]
            [Range(100, 2000, ErrorMessage = "Max Words Per Chunk must be between 100 and 2000")]
            public int MaxWordsPerChunk { get; set; }

            [Required]
            [Range(0, 10, ErrorMessage = "Overlap Sentences must be between 0 and 10")]
            public int OverlapSentences { get; set; }

            [Required]
            public string Strategy { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var subject = _subjectService.GetSubjectById(SubjectId);
            if (subject == null)
            {
                return NotFound();
            }

            SubjectName = subject.Name;

            var config = await _chunkConfigService.GetForSubjectAsync(SubjectId);

            Input = new ChunkConfigViewModel
            {
                MaxWordsPerChunk = config.MaxWordsPerChunk,
                OverlapSentences = config.OverlapSentences,
                Strategy = config.Strategy
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var subject = _subjectService.GetSubjectById(SubjectId);
            if (subject == null)
            {
                return NotFound();
            }
            SubjectName = subject.Name;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var config = new SubjectChunkConfig
            {
                SubjectId = SubjectId,
                MaxWordsPerChunk = Input.MaxWordsPerChunk,
                OverlapSentences = Input.OverlapSentences,
                Strategy = Input.Strategy
            };

            await _chunkConfigService.SaveAsync(config);
            
            TempData["SuccessMessage"] = "Chunk configuration saved successfully.";
            return RedirectToPage("/Subject/Index");
        }
    }
}
