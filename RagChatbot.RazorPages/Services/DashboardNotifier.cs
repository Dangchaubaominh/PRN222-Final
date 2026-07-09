using Microsoft.AspNetCore.SignalR;
using RagChatbot.RazorPages.Hubs;

namespace RagChatbot.RazorPages.Services
{
    public class DashboardNotifier : IDashboardNotifier
    {
        private readonly IHubContext<DashboardHub> _hub;

        public DashboardNotifier(IHubContext<DashboardHub> hub)
        {
            _hub = hub;
        }

        public Task StatsChangedAsync() => _hub.Clients.All.SendAsync("StatsChanged");
    }
}
