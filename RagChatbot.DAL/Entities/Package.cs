using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagChatbot.DAL.Entities
{
    [Table("Packages")]
    public class Package
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Name { get; set; }

        // Giá theo VND
        [Column(TypeName = "numeric(12,2)")]
        public decimal Price { get; set; }

        // Hạn mức token/tháng của gói
        public long TokenQuota { get; set; }

        // Danh sách model được phép, CSV các model id (vd: "gemini-2.5-flash-lite,gemini-2.5-flash")
        [Required]
        [StringLength(200)]
        [Column(TypeName = "varchar(200)")]
        public string AllowedModels { get; set; }

        // Số ngày hiệu lực của một chu kỳ gói
        public int DurationDays { get; set; } = 30;

        public bool IsActive { get; set; } = true;
    }
}
