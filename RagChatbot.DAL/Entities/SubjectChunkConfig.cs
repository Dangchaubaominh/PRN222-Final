using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagChatbot.DAL.Entities
{
    public class SubjectChunkConfig
    {
        [Key]
        public Guid SubjectId { get; set; }

        public int MaxWordsPerChunk { get; set; } = 400;

        public int OverlapSentences { get; set; } = 2;

        [Required]
        [MaxLength(50)]
        public string Strategy { get; set; } = "Semantic";

        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; }
    }
}
