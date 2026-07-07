using System;
using System.ComponentModel.DataAnnotations;

namespace RagChatbot.DAL.Entities
{
    public class QuizResult
    {
        [Key]
        public int Id { get; set; }

        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int Score { get; set; }
        public int TotalQuestions { get; set; }

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
