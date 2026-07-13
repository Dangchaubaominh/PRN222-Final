using System;

namespace RagChatbot.DAL.Entities
{
    /// <summary>
    /// Ghi lại lượng token tiêu thụ mỗi lần người dùng chat với AI.
    /// Dùng để tính quota còn lại và thống kê cho Dashboard.
    /// </summary>
    public class TokenUsageLog
    {
        public int Id { get; set; }

        /// <summary>Người dùng thực hiện chat</summary>
        public int UserId { get; set; }

        /// <summary>Môn học liên quan (null nếu không thuộc môn nào)</summary>
        public Guid? SubjectId { get; set; }

        /// <summary>Model Gemini đã dùng, vd: "gemini-2.5-flash"</summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>Số token trong prompt (câu hỏi + ngữ cảnh RAG)</summary>
        public int PromptTokens { get; set; }

        /// <summary>Số token trong câu trả lời của AI</summary>
        public int CompletionTokens { get; set; }

        /// <summary>Tổng token = PromptTokens + CompletionTokens</summary>
        public int TotalTokens { get; set; }

        /// <summary>Thời điểm ghi log (UTC)</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User? User { get; set; }
    }
}
