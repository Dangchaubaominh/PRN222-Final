using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Chat
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IChatMessageService _history;
        private readonly IUserSubjectService _userSubjectService;

        public IndexModel(IChatMessageService history, IUserSubjectService userSubjectService)
        {
            _history = history;
            _userSubjectService = userSubjectService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        public IEnumerable<ChatMessageDto> History { get; set; } = new List<ChatMessageDto>();

        public IActionResult OnGet()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Chỉ Admin hoặc thành viên môn học mới vào được phòng chat
            if (!User.IsInRole("Admin") && !_userSubjectService.IsAssigned(userId, SubjectId))
                return Forbid();

            History = _history.GetHistory(userId, SubjectId);
            return Page();
        }
    }
}
