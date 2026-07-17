using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagChatbot.DAL.Entities
{
    [Table("UserSubscriptions")]
    public class UserSubscription
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int PackageId { get; set; }

        public DateTime StartAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpireAt { get; set; }

        // Token đã dùng trong chu kỳ hiện tại
        public long TokensUsed { get; set; } = 0;

        // "Active" | "Expired"
        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "Active";
    }
}
