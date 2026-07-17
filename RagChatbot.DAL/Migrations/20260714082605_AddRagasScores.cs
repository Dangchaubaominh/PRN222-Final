using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRagasScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnswerRelevancy",
                table: "BenchmarkResults",
                type: "numeric(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContextPrecision",
                table: "BenchmarkResults",
                type: "numeric(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContextRecall",
                table: "BenchmarkResults",
                type: "numeric(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextRetrieved",
                table: "BenchmarkResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Faithfulness",
                table: "BenchmarkResults",
                type: "numeric(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroundTruth",
                table: "BenchmarkResults",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerRelevancy",
                table: "BenchmarkResults");

            migrationBuilder.DropColumn(
                name: "ContextPrecision",
                table: "BenchmarkResults");

            migrationBuilder.DropColumn(
                name: "ContextRecall",
                table: "BenchmarkResults");

            migrationBuilder.DropColumn(
                name: "ContextRetrieved",
                table: "BenchmarkResults");

            migrationBuilder.DropColumn(
                name: "Faithfulness",
                table: "BenchmarkResults");

            migrationBuilder.DropColumn(
                name: "GroundTruth",
                table: "BenchmarkResults");
        }
    }
}
