using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Implements;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Pages.Member
{
    [Authorize(Roles = "Admin, Lecturer")]
    public class AddModel : PageModel
    {
        private readonly IUserSubjectService _userSubjectService;
        private readonly ISubjectService _subjectService;
        private readonly IRealtimeNotifier _notificationService;
        private readonly IHubContext<SubjectHub> _subjectHub;
        private readonly IDashboardNotifier _dashboard;

        public AddModel(
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

        [BindProperty]
        public List<int> SelectedUserIds { get; set; } = new();

        public SubjectDto? Subject { get; set; }
        public IEnumerable<UserManageDto> AddableUsers { get; set; } = new List<UserManageDto>();
        public int TeacherCount { get; set; }
        public int TeacherLimit { get; set; }

        public IActionResult OnGet()
        {
            Subject = _subjectService.GetSubjectById(SubjectId);
            if (Subject == null) return NotFound();

            string requesterRole = User.IsInRole("Admin") ? "Admin" : "Lecturer";
            AddableUsers = _userSubjectService.GetAddableUsers(SubjectId, requesterRole);

            TeacherCount = _userSubjectService.CountTeachersInSubject(SubjectId);
            TeacherLimit = UserSubjectService.MaxTeachersPerSubject;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedUserIds == null || SelectedUserIds.Count == 0)
                return RedirectToPage("Add", new { subjectId = SubjectId });

            if (User.IsInRole("Lecturer"))
            {
                var allowedIds = _userSubjectService.GetAddableUsers(SubjectId, "Lecturer")
                                                    .Select(u => u.Id).ToHashSet();
                if (!SelectedUserIds.All(id => allowedIds.Contains(id)))
                    return Forbid();
            }

            int added = 0, limitBlocked = 0;
            var addedUserIds = new List<int>();
            foreach (var userId in SelectedUserIds)
            {
                var result = _userSubjectService.Assign(userId, SubjectId);
                if (result == AssignResult.Success) { added++; addedUserIds.Add(userId); }
                else if (result == AssignResult.TeacherLimitReached) limitBlocked++;
            }

            // Thông báo real-time cho từng người vừa được thêm vào môn học
            if (addedUserIds.Count > 0)
            {
                var subject = _subjectService.GetSubjectById(SubjectId);
                string subjectLabel = subject == null ? "một môn học" : $"{subject.Code} — {subject.Name}";
                await _notificationService.NotifyUsersAsync(
                    addedUserIds,
                    $"Bạn vừa được thêm vào môn học {subjectLabel}.",
                    "success");

                // Cập nhật danh sách thành viên cho mọi người đang xem môn học này
                await _subjectHub.Clients.Group(SubjectHub.MembersGroup(SubjectId))
                                 .SendAsync("MembersChanged");

                // Cập nhật danh sách môn học cho các user vừa được thêm nếu họ đang mở trang Môn học.
                // Client tự reload partial theo phân quyền nên chỉ thấy môn được phép xem.
                await _subjectHub.Clients.Group(SubjectHub.SubjectListGroup)
                                 .SendAsync("SubjectListChanged");

                // Cập nhật số liệu dashboard (số môn/tài liệu của sinh viên vừa được thêm)
                await _dashboard.StatsChangedAsync();
            }

            if (limitBlocked > 0 && added == 0)
                TempData["ErrorMessage"] = $"Không thể thêm: môn học đã đạt tối đa {UserSubjectService.MaxTeachersPerSubject} giảng viên.";
            else if (limitBlocked > 0)
                TempData["WarningMessage"] = $"Đã thêm {added} thành viên. {limitBlocked} giảng viên bị từ chối vì môn học đã đạt giới hạn {UserSubjectService.MaxTeachersPerSubject} giảng viên.";
            else
                TempData["SuccessMessage"] = $"Đã thêm {added} thành viên vào môn học.";

            return RedirectToPage("Index", new { subjectId = SubjectId });
        }
    }
}
