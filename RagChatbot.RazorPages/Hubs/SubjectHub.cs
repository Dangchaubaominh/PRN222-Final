using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RagChatbot.RazorPages.Hubs
{
    /// <summary>
    /// Hub cập nhật danh sách real-time:
    /// - Nhóm "members-{subjectId}": ai đang xem trang Thành viên của một môn học.
    /// - Nhóm "subject-list": ai đang xem danh sách Môn học.
    /// Khi dữ liệu đổi, server chỉ phát tín hiệu; client tự nạp lại phần bảng
    /// (partial) để giữ đúng logic phân quyền phía server.
    /// </summary>
    [Authorize]
    public class SubjectHub : Hub
    {
        public const string SubjectListGroup = "subject-list";
        public static string MembersGroup(Guid subjectId) => $"members-{subjectId}";

        public Task JoinMembers(Guid subjectId)
            => Groups.AddToGroupAsync(Context.ConnectionId, MembersGroup(subjectId));

        public Task JoinSubjectList()
            => Groups.AddToGroupAsync(Context.ConnectionId, SubjectListGroup);
    }
}
