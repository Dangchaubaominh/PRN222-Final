using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RagChatbot.Tests")]

namespace RagChatbot.BLL.Services.Implements
{
    /// <summary>
    /// Tích hợp MoMo (cổng test). Tạo đơn: POST server→server tới MoMo lấy payUrl để
    /// redirect người dùng. Chữ ký HMAC-SHA256 trên chuỗi field theo THỨ TỰ CỐ ĐỊNH của MoMo.
    /// Sandbox có sẵn creds test công khai nên chạy được ngay.
    /// </summary>
    public class MoMoService : IPaymentService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly IConfiguration _config;
        private readonly IPackageRepository _packageRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ISubscriptionService _subscriptionService;

        public MoMoService(
            IConfiguration config,
            IPackageRepository packageRepo,
            IPaymentRepository paymentRepo,
            ISubscriptionService subscriptionService)
        {
            _config = config;
            _packageRepo = packageRepo;
            _paymentRepo = paymentRepo;
            _subscriptionService = subscriptionService;
        }

        private string PartnerCode => _config["Momo:PartnerCode"] ?? "MOMO";
        private string AccessKey => _config["Momo:AccessKey"] ?? "F8BBA842ECF85";
        private string SecretKey => _config["Momo:SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";
        private string Endpoint => _config["Momo:Endpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
        private string RedirectUrl => _config["Momo:RedirectUrl"] ?? "http://localhost:5136/Payment/Return";
        private string IpnUrl => _config["Momo:IpnUrl"] ?? "http://localhost:5136/Payment/Ipn";

        public IEnumerable<PaymentOrder> GetUserOrders(int userId) => _paymentRepo.GetByUser(userId);

        public async Task<string> CreatePaymentUrl(int userId, int packageId, string clientIp)
        {
            var package = _packageRepo.GetById(packageId)
                ?? throw new InvalidOperationException("Gói không tồn tại.");

            string orderId = DateTime.Now.Ticks.ToString();
            string requestId = Guid.NewGuid().ToString();
            string amount = ((long)package.Price).ToString(CultureInfo.InvariantCulture);
            string orderInfo = $"Thanh toan goi {package.Name}";
            string extraData = "";
            string requestType = "captureWallet";

            _paymentRepo.Add(new PaymentOrder
            {
                UserId = userId,
                PackageId = packageId,
                Amount = package.Price,
                VnpTxnRef = orderId,   // lưu MoMo orderId
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });

            // Chuỗi ký tạo đơn — THỨ TỰ FIELD theo đúng tài liệu MoMo
            string raw =
                $"accessKey={AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={IpnUrl}" +
                $"&orderId={orderId}&orderInfo={orderInfo}&partnerCode={PartnerCode}" +
                $"&redirectUrl={RedirectUrl}&requestId={requestId}&requestType={requestType}";
            string signature = HmacSha256(SecretKey, raw);

            var body = new
            {
                partnerCode = PartnerCode,
                accessKey = AccessKey,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = RedirectUrl,
                ipnUrl = IpnUrl,
                extraData,
                requestType,
                signature,
                lang = "vi"
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(Endpoint, content);
            string json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            int resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
            if (resultCode == 0 && root.TryGetProperty("payUrl", out var payUrl))
                return payUrl.GetString() ?? throw new InvalidOperationException("MoMo không trả payUrl.");

            string message = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            throw new InvalidOperationException($"MoMo tạo đơn thất bại: {message} (resultCode {resultCode}).");
        }

        public PaymentResult HandleReturn(IReadOnlyDictionary<string, string> p)
        {
            if (!p.TryGetValue("signature", out var receivedSig))
                return new PaymentResult { Success = false, Message = "Thiếu chữ ký giao dịch." };

            string V(string k) => p.GetValueOrDefault(k, "");

            // Chuỗi ký callback — THỨ TỰ FIELD cố định của MoMo
            string raw =
                $"accessKey={AccessKey}&amount={V("amount")}&extraData={V("extraData")}&message={V("message")}" +
                $"&orderId={V("orderId")}&orderInfo={V("orderInfo")}&orderType={V("orderType")}" +
                $"&partnerCode={V("partnerCode")}&payType={V("payType")}&requestId={V("requestId")}" +
                $"&responseTime={V("responseTime")}&resultCode={V("resultCode")}&transId={V("transId")}";

            if (!string.Equals(HmacSha256(SecretKey, raw), receivedSig, StringComparison.InvariantCultureIgnoreCase))
                return new PaymentResult { Success = false, Message = "Chữ ký không hợp lệ." };

            var order = _paymentRepo.GetByTxnRef(V("orderId"));
            if (order == null)
                return new PaymentResult { Success = false, Message = "Không tìm thấy đơn hàng." };

            if (order.Status == "Paid")
                return new PaymentResult { Success = true, Message = "Đơn đã được thanh toán.", PackageId = order.PackageId, Amount = order.Amount };

            if (!long.TryParse(V("amount"), out var amount) || amount != (long)order.Amount)
            {
                order.Status = "Failed";
                _paymentRepo.Update(order);
                return new PaymentResult { Success = false, Message = "Số tiền không khớp." };
            }

            if (V("resultCode") == "0")
            {
                order.Status = "Paid";
                order.PaidAt = DateTime.UtcNow;
                _paymentRepo.Update(order);
                _subscriptionService.ActivateOrRenew(order.UserId, order.PackageId);
                return new PaymentResult { Success = true, Message = "Thanh toán thành công.", PackageId = order.PackageId, Amount = order.Amount };
            }

            order.Status = "Failed";
            _paymentRepo.Update(order);
            return new PaymentResult { Success = false, Message = $"Thanh toán thất bại ({V("message")})." };
        }

        internal static string HmacSha256(string key, string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
