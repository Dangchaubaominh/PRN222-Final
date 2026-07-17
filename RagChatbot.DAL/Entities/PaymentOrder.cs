using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagChatbot.DAL.Entities
{
    [Table("PaymentOrders")]
    public class PaymentOrder
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int PackageId { get; set; }

        // Số tiền theo VND
        [Column(TypeName = "numeric(12,2)")]
        public decimal Amount { get; set; }

        // Mã giao dịch gửi sang VNPay (vnp_TxnRef) — duy nhất
        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string VnpTxnRef { get; set; }

        // "Pending" | "Paid" | "Failed"
        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
    }
}
