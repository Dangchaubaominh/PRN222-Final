using System.Collections.Generic;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IPaymentService
    {
        // Tạo đơn Pending + trả về URL thanh toán VNPay để redirect người dùng
        string CreatePaymentUrl(int userId, int packageId, string clientIp);

        // Xử lý tham số trả về từ VNPay (đã tách khỏi HttpRequest): xác thực chữ ký,
        // cập nhật đơn, kích hoạt gói. Truyền vào toàn bộ query vnp_* dạng dictionary.
        PaymentResult HandleReturn(IReadOnlyDictionary<string, string> vnpParams);
    }
}
