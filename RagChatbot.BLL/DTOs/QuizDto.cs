using System;
using System.Collections.Generic;

namespace RagChatbot.BLL.DTOs
{
    public class QuizDto
    {
        public int Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }

        public List<QuizQuestionDto> Questions { get; set; } = new List<QuizQuestionDto>();
    }
}
