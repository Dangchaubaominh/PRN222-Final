using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;

namespace RagChatbot.RazorPages.Pages.Packages
{
    [Authorize]
    public class BuyModel : PageModel
    {
        private readonly IPackageService _packageService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentService _paymentService;

        public BuyModel(IPackageService packageService, ISubscriptionService subscriptionService, IPaymentService paymentService)
        {
            _packageService = packageService;
            _subscriptionService = subscriptionService;
            _paymentService = paymentService;
        }

        public List<PackageDto> Packages { get; set; } = new();
        public UserSubscription? Current { get; set; }
        public string? CurrentPackageName { get; set; }
        public long RemainingQuota { get; set; }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public void OnGet()
        {
            Packages = _packageService.GetActive().ToList();
            Current = _subscriptionService.GetActive(UserId);
            if (Current != null)
            {
                CurrentPackageName = _packageService.GetById(Current.PackageId)?.Name;
                RemainingQuota = _subscriptionService.GetRemainingQuota(UserId);
            }
        }

        public IActionResult OnPostBuy(int packageId)
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var url = _paymentService.CreatePaymentUrl(UserId, packageId, ip);
            return Redirect(url);
        }
    }
}
