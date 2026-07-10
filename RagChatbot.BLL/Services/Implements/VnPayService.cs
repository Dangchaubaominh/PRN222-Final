using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RagChatbot.Tests")]

namespace RagChatbot.BLL.Services.Implements
{
    /// <summary>
    /// Tích hợp VNPay sandbox. Ký HMAC-SHA512 trên chuỗi tham số vnp_* đã sắp xếp
    /// theo tên field (ordinal) và URL-encode — đúng chuẩn VNPay.
    /// </summary>
    public class VnPayService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IPackageRepository _packageRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ISubscriptionService _subscriptionService;

        public VnPayService(
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

        public IEnumerable<PaymentOrder> GetUserOrders(int userId) => _paymentRepo.GetByUser(userId);

        public string CreatePaymentUrl(int userId, int packageId, string clientIp)
        {
            var package = _packageRepo.GetById(packageId)
                ?? throw new InvalidOperationException("Gói không tồn tại.");

            string tmnCode = _config["Vnpay:TmnCode"] ?? "";
            string hashSecret = _config["Vnpay:HashSecret"] ?? "";
            string baseUrl = _config["Vnpay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vnpaypay.html";
            string returnUrl = _config["Vnpay:ReturnUrl"] ?? "";

            // Mã giao dịch duy nhất
            string txnRef = DateTime.Now.Ticks.ToString();

            // Lưu đơn ở trạng thái chờ
            _paymentRepo.Add(new PaymentOrder
            {
                UserId = userId,
                PackageId = packageId,
                Amount = package.Price,
                VnpTxnRef = txnRef,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });

            var now = VnNow();
            var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = ((long)(package.Price * 100)).ToString(CultureInfo.InvariantCulture),
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = txnRef,
                ["vnp_OrderInfo"] = $"Thanh toan goi {package.Name}",
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vn",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
            };

            string signData = BuildQuery(data);
            string secureHash = HmacSha512(hashSecret, signData);
            return $"{baseUrl}?{signData}&vnp_SecureHash={secureHash}";
        }

        public PaymentResult HandleReturn(IReadOnlyDictionary<string, string> vnpParams)
        {
            string hashSecret = _config["Vnpay:HashSecret"] ?? "";

            if (!vnpParams.TryGetValue("vnp_SecureHash", out var receivedHash))
                return new PaymentResult { Success = false, Message = "Thiếu chữ ký giao dịch." };

            // Ký lại trên các tham số vnp_* (bỏ hash), so khớp
            var signParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in vnpParams)
            {
                if (kv.Key == "vnp_SecureHash" || kv.Key == "vnp_SecureHashType") continue;
                if (kv.Key.StartsWith("vnp_") && !string.IsNullOrEmpty(kv.Value))
                    signParams[kv.Key] = kv.Value;
            }

            string computed = HmacSha512(hashSecret, BuildQuery(signParams));
            if (!string.Equals(computed, receivedHash, StringComparison.InvariantCultureIgnoreCase))
                return new PaymentResult { Success = false, Message = "Chữ ký không hợp lệ." };

            string txnRef = vnpParams.GetValueOrDefault("vnp_TxnRef", "");
            var order = _paymentRepo.GetByTxnRef(txnRef);
            if (order == null)
                return new PaymentResult { Success = false, Message = "Không tìm thấy đơn hàng." };

            // Idempotent: đã xử lý rồi thì trả kết quả cũ
            if (order.Status == "Paid")
                return new PaymentResult { Success = true, Message = "Đơn đã được thanh toán.", PackageId = order.PackageId, Amount = order.Amount };

            // Kiểm tra số tiền khớp (đơn vị VNPay = *100)
            long expected = (long)(order.Amount * 100);
            if (!long.TryParse(vnpParams.GetValueOrDefault("vnp_Amount", ""), out var vnpAmount) || vnpAmount != expected)
            {
                order.Status = "Failed";
                _paymentRepo.Update(order);
                return new PaymentResult { Success = false, Message = "Số tiền không khớp." };
            }

            string responseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode", "");
            if (responseCode == "00")
            {
                order.Status = "Paid";
                order.PaidAt = DateTime.UtcNow;
                _paymentRepo.Update(order);
                _subscriptionService.ActivateOrRenew(order.UserId, order.PackageId);
                return new PaymentResult { Success = true, Message = "Thanh toán thành công.", PackageId = order.PackageId, Amount = order.Amount };
            }

            order.Status = "Failed";
            _paymentRepo.Update(order);
            return new PaymentResult { Success = false, Message = $"Thanh toán thất bại (mã {responseCode})." };
        }

        // Chuỗi ký: key=value nối bằng &, đã URL-encode, theo thứ tự key ordinal
        internal static string BuildQuery(SortedDictionary<string, string> data)
            => string.Join("&", data.Select(kv =>
                $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

        private static string HmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static DateTime VnNow()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(7); // GMT+7 fallback
            }
        }
    }
}
