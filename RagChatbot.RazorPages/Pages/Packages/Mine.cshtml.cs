using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;

namespace RagChatbot.RazorPages.Pages.Packages
{
    [Authorize]
    public class MineModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPackageService _packageService;
        private readonly IPaymentService _paymentService;
        private readonly ITokenUsageService _tokenUsage;

        public MineModel(ISubscriptionService subscriptionService, IPackageService packageService, IPaymentService paymentService, ITokenUsageService tokenUsage)
        {
            _subscriptionService = subscriptionService;
            _packageService = packageService;
            _paymentService = paymentService;
            _tokenUsage = tokenUsage;
        }

        public long TokensThisMonth { get; set; }

        public UserSubscription? Current { get; set; }
        public string? CurrentPackageName { get; set; }
        public long RemainingQuota { get; set; }
        public long TotalQuota { get; set; }
        public List<PaymentOrder> Orders { get; set; } = new();
        public Dictionary<int, string> PackageNames { get; set; } = new();

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task OnGet()
        {
            PackageNames = _packageService.GetAll().ToDictionary(p => p.Id, p => p.Name);

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            TokensThisMonth = await _tokenUsage.TotalTokens(UserId, monthStart, now);

            Current = _subscriptionService.GetActive(UserId);
            if (Current != null)
            {
                CurrentPackageName = PackageNames.GetValueOrDefault(Current.PackageId, "—");
                RemainingQuota = _subscriptionService.GetRemainingQuota(UserId);
                var pkg = _packageService.GetById(Current.PackageId);
                TotalQuota = pkg?.TokenQuota ?? 0;
            }

            Orders = _paymentService.GetUserOrders(UserId).ToList();
        }
    }
}
