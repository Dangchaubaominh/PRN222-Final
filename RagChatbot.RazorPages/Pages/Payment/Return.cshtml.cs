using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Payment
{
    [Authorize]
    public class ReturnModel : PageModel
    {
        private readonly IPaymentService _paymentService;

        public ReturnModel(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public PaymentResult Result { get; set; } = new();

        public void OnGet()
        {
            // Chuyển query vnp_* từ HttpRequest sang dictionary để BLL xử lý (BLL không phụ thuộc HTTP)
            var dict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            Result = _paymentService.HandleReturn(dict);
        }
    }
}
