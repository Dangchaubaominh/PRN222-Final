using System;

namespace RagChatbot.BLL.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Status { get; set; }
        public string? ProgressMessage { get; set; }
        public int AccessLevel { get; set; }
        public int? UploadedById { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
