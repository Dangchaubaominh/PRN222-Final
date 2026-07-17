using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagChatbot.DAL.Entities
{
    public class BenchmarkResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RunId { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }

        public string? GroundTruth { get; set; }

        public string? ContextRetrieved { get; set; }

        [Column(TypeName = "decimal(5, 4)")]
        public decimal? Faithfulness { get; set; }

        [Column(TypeName = "decimal(5, 4)")]
        public decimal? AnswerRelevancy { get; set; }

        [Column(TypeName = "decimal(5, 4)")]
        public decimal? ContextPrecision { get; set; }

        [Column(TypeName = "decimal(5, 4)")]
        public decimal? ContextRecall { get; set; }

        public int LatencyMs { get; set; }

        public int TotalTokens { get; set; }

        [Column(TypeName = "decimal(18, 6)")]
        public decimal EstimatedCost { get; set; }

        [ForeignKey("RunId")]
        public virtual BenchmarkRun Run { get; set; }
    }
}
