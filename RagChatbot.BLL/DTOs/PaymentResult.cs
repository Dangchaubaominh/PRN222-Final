namespace RagChatbot.BLL.DTOs
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? PackageId { get; set; }
        public decimal Amount { get; set; }
    }
}
