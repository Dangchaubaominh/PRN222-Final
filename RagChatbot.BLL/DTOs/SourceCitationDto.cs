using System;

namespace RagChatbot.BLL.DTOs
{
    public class SourceCitationDto
    {
        public Guid DocumentId { get; init; }
        public Guid ChunkId { get; init; }
        public string FileName { get; init; } = "";
        public int? PageNumber { get; init; }
        public int ChunkIndex { get; init; }
        public string Snippet { get; init; } = "";
    }
}
