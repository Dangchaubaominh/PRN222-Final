using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RagChatbot.RazorPages.Hubs
{
    /// <summary>
    /// Hub đẩy trạng thái xử lý tài liệu real-time. Mỗi môn học là một group
    /// ("subject-{id}"); client đang xem danh sách tài liệu của môn nào sẽ join
    /// group đó để nhận cập nhật trạng thái (Đang xử lý → Đã học xong / Lỗi).
    /// </summary>
    [Authorize]
    public class DocumentHub : Hub
    {
        public static string GroupName(Guid subjectId) => $"subject-{subjectId}";

        public Task JoinSubject(Guid subjectId)
            => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(subjectId));

        public Task LeaveSubject(Guid subjectId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(subjectId));
    }
}
