using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Implements
{
    public class ChatbotService : IChatbotService
    {
        private readonly IDocumentChunkRepository _chunkRepository;
        private readonly IAIService _aiService;

        public ChatbotService(IDocumentChunkRepository chunkRepository, IAIService aiService)
        {
            _chunkRepository = chunkRepository;
            _aiService = aiService;
        }

        public async Task<ChatResult> AskAsync(
            Guid subjectId,
            string userMessage,
            int currentUserId,
            string currentUserRole,
            string model,
            CancellationToken cancellationToken = default)
        {
            string? prepError = null;
            List<DocumentChunk> chunks = new();

            try
            {
                float[] questionVector = await _aiService.GenerateEmbeddingAsync(userMessage);
                chunks = (await _chunkRepository.SearchSimilarChunksAsync(subjectId, questionVector, currentUserId, currentUserRole, topK: 3)).ToList();
            }
            catch (Exception ex)
            {
                prepError = $"Hệ thống gặp lỗi nội bộ: {ex.Message}";
            }

            if (prepError != null)
                return new ChatResult { Answer = Single(prepError) };

            if (!chunks.Any())
                return new ChatResult { Answer = Single("Môn học này hiện chưa có tài liệu nào. Vui lòng upload tài liệu trước khi hỏi.") };

            var sources = chunks
                .Select((chunk, index) => new SourceCitationDto
                {
                    DocumentId = chunk.DocumentId,
                    ChunkId = chunk.Id,
                    FileName = chunk.Document?.FileName ?? "Unknown document",
                    PageNumber = chunk.PageNumber,
                    ChunkIndex = chunk.ChunkIndex ?? index + 1,
                    Snippet = BuildSnippet(chunk.TextContent)
                })
                .ToList();

            string contextText = string.Join("\n\n---\n\n", chunks.Select((chunk, index) =>
            {
                int sourceNumber = index + 1;
                string fileName = chunk.Document?.FileName ?? "Unknown document";
                string pageText = chunk.PageNumber.HasValue ? $", trang {chunk.PageNumber.Value}" : "";
                int chunkIndex = chunk.ChunkIndex ?? sourceNumber;

                return $"[Nguồn {sourceNumber}: file \"{fileName}\"{pageText}, đoạn {chunkIndex}]\n{chunk.TextContent}";
            }));

            string finalPrompt = $"""
            TÀI LIỆU CUNG CẤP:
            {contextText}

            CÂU HỎI CỦA NGƯỜI DÙNG:
            {userMessage}

            YÊU CẦU TRẢ LỜI:
            - Trả lời dựa trên tài liệu được cung cấp.
            - Khi nêu thông tin lấy từ tài liệu, có thể ghi số nguồn dạng [Nguồn 1], [Nguồn 2] nếu phù hợp.
            - Không tự tạo tên file, số trang hoặc số đoạn ngoài metadata nguồn ở trên.
            """;

            // ChatResult.Usage được set qua callback sau khi stream hoàn tất
            var chatResult = new ChatResult { Sources = sources };

            chatResult.Answer = _aiService.GenerateChatResponseStreamAsync(
                finalPrompt,
                model,
                usage => chatResult.Usage = usage,   // ChatHub lấy Usage sau khi foreach xong
                cancellationToken);

            return chatResult;
        }

        private static async IAsyncEnumerable<string> Single(string message)
        {
            yield return message;
            await Task.CompletedTask;
        }

        private static string BuildSnippet(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string normalized = Regex.Replace(text, @"\s+", " ").Trim();
            const int maxLength = 240;

            return normalized.Length <= maxLength
                ? normalized
                : normalized.Substring(0, maxLength).TrimEnd() + "...";
        }
    }
}
