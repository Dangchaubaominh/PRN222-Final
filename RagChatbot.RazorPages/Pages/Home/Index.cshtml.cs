using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.Pages.Home
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IUserSubjectService _userSubjectService;
        private readonly IUserService _userService;
        private readonly IDocumentService _documentService;
        private readonly IPresenceTracker _presence;

        public IndexModel(
            ISubjectService subjectService,
            IUserSubjectService userSubjectService,
            IUserService userService,
            IDocumentService documentService,
            IPresenceTracker presence)
        {
            _subjectService = subjectService;
            _userSubjectService = userSubjectService;
            _userService = userService;
            _documentService = documentService;
            _presence = presence;
        }

        public int SubjectCount { get; set; }
        public int UserCount { get; set; }
        public int DocumentCount { get; set; }
        public int OnlineCount { get; set; }

        // Dữ liệu cho biểu đồ dashboard (Chart.js)
        public List<string> SubjectNames { get; set; } = new();
        public List<int> SubjectDocCounts { get; set; } = new();
        public int[] RoleCounts { get; set; } = new int[3]; // [Admin, Lecturer, Student]

        private void LoadStats()
        {
            OnlineCount = _presence.OnlineCount;

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            string role = User.FindFirstValue(ClaimTypes.Role) ?? "";

            if (User.IsInRole("Admin"))
            {
                var subjects = _subjectService.GetAllSubjects().ToList();
                SubjectCount = subjects.Count;
                DocumentCount = _documentService.CountAllDocuments();

                var users = _userService.GetAllUsers().ToList();
                UserCount = users.Count;
                RoleCounts = new[]
                {
                    users.Count(u => u.Role == "Admin"),
                    users.Count(u => u.Role == "Lecturer"),
                    users.Count(u => u.Role == "Student"),
                };

                // ponytail: N truy vấn theo số môn (N nhỏ). Nếu môn nhiều, thêm CountBySubject vào IDocumentService.
                foreach (var s in subjects)
                {
                    SubjectNames.Add(s.Code);
                    SubjectDocCounts.Add(_documentService.GetDocumentsBySubject(s.Id, userId, role).Count());
                }
                return;
            }

            var assigned = _userSubjectService.GetAssignedSubjects(userId).ToList();
            SubjectCount = assigned.Count;
            UserCount = 0;
            foreach (var s in assigned)
            {
                int docs = _documentService.GetDocumentsBySubject(s.Id, userId, role).Count();
                DocumentCount += docs;
                SubjectNames.Add(s.Code);
                SubjectDocCounts.Add(docs);
            }
        }

        public void OnGet() => LoadStats();

        public IActionResult OnGetStats()
        {
            LoadStats();
            return new JsonResult(new
            {
                subjects = SubjectCount,
                users = UserCount,
                documents = DocumentCount,
                online = OnlineCount
            });
        }
    }
}
