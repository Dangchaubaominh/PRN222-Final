using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;
using RagChatbot.RazorPages.Services;
using System.Security.Claims;

namespace RagChatbot.RazorPages.Pages.Member
{
    [Authorize(Roles = "Admin, Lecturer")]
    public class IndexModel : PageModel
    {
        private readonly IUserSubjectService _userSubjectService;
        private readonly ISubjectService _subjectService;
        private readonly IRealtimeNotifier _notificationService;
        private readonly IHubContext<SubjectHub> _subjectHub;
        private readonly IDashboardNotifier _dashboard;

        public IndexModel(
            IUserSubjectService userSubjectService,
            ISubjectService subjectService,
            IRealtimeNotifier notificationService,
            IHubContext<SubjectHub> subjectHub,
            IDashboardNotifier dashboard)
        {
            _userSubjectService = userSubjectService;
            _subjectService = subjectService;
            _notificationService = notificationService;
            _subjectHub = subjectHub;
            _dashboard = dashboard;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SubjectId { get; set; }

        public SubjectDto? Subject { get; set; }
        public IEnumerable<UserManageDto> Members { get; set; } = new List<UserManageDto>();

        // Nạp Subject + Members kèm kiểm tra quyền; trả về kết quả lỗi nếu có, null nếu OK.
        private IActionResult? LoadAndGuard()
        {
            Subject = _subjectService.GetSubjectById(SubjectId);
            if (Subject == null) return NotFound();

            // Giảng viên chỉ quản lý môn học mình được gán
            if (User.IsInRole("Lecturer"))
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var assigned = _userSubjectService.GetAssignedSubjects(userId);
                bool isMine = assigned.Any(s => s.Id == SubjectId);
                if (!isMine) return Forbid();
            }

            Members = _userSubjectService.GetAssignedUsers(SubjectId);
            return null;
        }

        public IActionResult OnGet()
        {
            var error = LoadAndGuard();
            return error ?? Page();
        }

        // Trả về phần bảng thành viên (gọi qua AJAX khi nhận MembersChanged)
        public IActionResult OnGetTablePartial()
        {
            var error = LoadAndGuard();
            return error ?? Partial("_MembersTable", this);
        }

        public async Task<IActionResult> OnPostRemoveAsync(Guid subjectId, int userId)
        {
            // Lecturer chỉ được xóa Student
            if (User.IsInRole("Lecturer"))
            {
                var members = _userSubjectService.GetAssignedUsers(subjectId);
                foreach (var m in members)
                {
                    if (m.Id == userId && m.Role != "Student")
                        return Forbid();
                }
            }

            _userSubjectService.Remove(userId, subjectId);

            // Thông báo real-time cho người bị gỡ khỏi môn học
            var subject = _subjectService.GetSubjectById(subjectId);
            string subjectLabel = subject == null ? "một môn học" : $"{subject.Code} — {subject.Name}";
            await _notificationService.NotifyUserAsync(
                userId,
                $"Bạn đã bị gỡ khỏi môn học {subjectLabel}.",
                "warning");

            // Cập nhật danh sách thành viên cho mọi người đang xem môn học này
            await _subjectHub.Clients.Group(SubjectHub.MembersGroup(subjectId))
                             .SendAsync("MembersChanged");

            // Cập nhật danh sách môn học cho user vừa bị gỡ nếu họ đang mở trang Môn học.
            // Client tự reload partial theo phân quyền nên môn bị gỡ sẽ biến mất.
            await _subjectHub.Clients.Group(SubjectHub.SubjectListGroup)
                             .SendAsync("SubjectListChanged");

            // Cập nhật số liệu dashboard (số môn/tài liệu của user vừa bị gỡ)
            await _dashboard.StatsChangedAsync();

            TempData["SuccessMessage"] = "Đã xóa thành viên khỏi môn học.";
            return RedirectToPage(new { subjectId });
        }
    }
}
