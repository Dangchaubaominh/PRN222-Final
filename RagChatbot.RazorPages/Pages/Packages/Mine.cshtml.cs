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

        public MineModel(ISubscriptionService subscriptionService, IPackageService packageService, IPaymentService paymentService)
        {
            _subscriptionService = subscriptionService;
            _packageService = packageService;
            _paymentService = paymentService;
        }

        public UserSubscription? Current { get; set; }
        public string? CurrentPackageName { get; set; }
        public long RemainingQuota { get; set; }
        public long TotalQuota { get; set; }
        public List<PaymentOrder> Orders { get; set; } = new();
        public Dictionary<int, string> PackageNames { get; set; } = new();

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public void OnGet()
        {
            PackageNames = _packageService.GetAll().ToDictionary(p => p.Id, p => p.Name);

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
