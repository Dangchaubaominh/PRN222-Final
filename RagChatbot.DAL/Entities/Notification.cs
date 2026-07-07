using System;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // Người nhận thông báo
        public int UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        // success | info | warning | error (khớp icon SweetAlert)
        [MaxLength(20)]
        public string Type { get; set; } = "info";

        public bool IsRead { get; set; }

        [MaxLength(500)]
        public string? LinkUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
