using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class Quiz
    {
        [Key]
        public int Id { get; set; }

        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CreatedById { get; set; }
        public User CreatedBy { get; set; }

        public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        public ICollection<QuizResult> Results { get; set; } = new List<QuizResult>();
    }
}
