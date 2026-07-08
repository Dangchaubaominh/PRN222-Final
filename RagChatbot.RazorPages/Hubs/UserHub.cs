using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RagChatbot.RazorPages.Hubs
{
    [Authorize(Roles = "Admin")]
    public class UserHub : Hub
    {
        public const string UserListGroup = "user-list";

        public Task JoinUserList()
            => Groups.AddToGroupAsync(Context.ConnectionId, UserListGroup);
    }
}