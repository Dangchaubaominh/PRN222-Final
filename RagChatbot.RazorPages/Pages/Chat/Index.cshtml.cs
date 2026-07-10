using System.Collections.Generic;
using System.Linq;
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
        private readonly ISubscriptionService _subscriptionService;

        public IndexModel(
            IChatMessageService history,
            IUserSubjectService userSubjectService,
            ISubscriptionService subscriptionService)
        {
            _history             = history;
            _userSubjectService  = userSubjectService;
            _subscriptionService = subscriptionService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        public IEnumerable<ChatMessageDto> History { get; set; } = new List<ChatMessageDto>();

        /// <summary>
        /// Danh sách model Gemini user được dùng theo gói subscription.
        /// Truyền xuống View để render dropdown chọn model.
        /// </summary>
        public IList<string> AllowedModels { get; set; } = new List<string>();

        public IActionResult OnGet()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Chỉ Admin hoặc thành viên môn học mới vào được phòng chat
            if (!User.IsInRole("Admin") && !_userSubjectService.IsAssigned(userId, SubjectId))
                return Forbid();

            History = _history.GetHistory(userId, SubjectId);

            // Lấy danh sách model theo gói — stub trả về tất cả model trong dev
            // Khi Bảo Minh xong: thay bằng subscription.GetActive(userId)?.Package?.AllowedModels.Split(',')
            AllowedModels = GetAllowedModels(userId);

            return Page();
        }

        private IList<string> GetAllowedModels(int userId)
        {
            // TODO: Khi Bảo Minh xong, thay bằng:
            // var sub = _subscriptionService.GetActive(userId);
            // if (sub == null) return new[] { "gemini-2.5-flash" };
            // return sub.Package.AllowedModels.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Tạm thời: hiện tất cả model để test
            return new List<string>
            {
                "gemini-2.5-flash",
                "gemini-2.5-pro"
            };
        }
    }
}

