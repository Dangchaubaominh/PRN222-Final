using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class QuizService : IQuizService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly IDocumentChunkRepository _chunkRepo;

        public QuizService(ApplicationDbContext context, IAIService aiService, IDocumentChunkRepository chunkRepo)
        {
            _context = context;
            _aiService = aiService;
            _chunkRepo = chunkRepo;
        }

        public async Task<(QuizResultDto Result, QuizDto Quiz)?> GetResultDetailAsync(int resultId)
        {
            var qr = await _context.QuizResults
                .Include(r => r.Quiz)
                .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(r => r.Id == resultId);

            if (qr == null) return null;

            var result = new QuizResultDto
            {
                Id = qr.Id,
                QuizId = qr.QuizId,
                UserId = qr.UserId,
                Score = qr.Score,
                TotalQuestions = qr.TotalQuestions,
                CompletedAt = qr.CompletedAt
            };

            var quiz = new QuizDto
            {
                Title = qr.Quiz.Title,
                Questions = qr.Quiz.Questions.Select(q => new QuizQuestionDto
                {
                    Content = q.Content,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption,
                    Explanation = q.Explanation
                }).ToList()
            };

            return (result, quiz);
        }

        public async Task<QuizDto> GenerateQuizAsync(Guid documentId, int numberOfQuestions, int userId)
        {
            // Lấy các chunk của document (giới hạn một số lượng để tránh quá dài)
            var chunks = _chunkRepo.GetByDocumentId(documentId).Take(50).ToList();
            if (!chunks.Any())
                throw new Exception("Tài liệu chưa được xử lý hoặc không có nội dung.");

            string documentContent = string.Join("\n\n", chunks.Select(c => c.TextContent));
            
            // Giới hạn text gửi đi để tránh vượt quá token của API
            if (documentContent.Length > 30000)
            {
                documentContent = documentContent.Substring(0, 30000);
            }

            string prompt = $@"
Dựa vào nội dung tài liệu sau, hãy tạo một bài trắc nghiệm gồm {numberOfQuestions} câu hỏi.
Yêu cầu bắt buộc:
1. Bạn CHỈ được trả về mảng JSON, KHÔNG KÈM TEXT NÀO KHÁC (không có ```json ... ```).
2. Định dạng JSON chính xác như sau:
[
  {{
    ""Question"": ""Nội dung câu hỏi?"",
    ""OptionA"": ""Đáp án A"",
    ""OptionB"": ""Đáp án B"",
    ""OptionC"": ""Đáp án C"",
    ""OptionD"": ""Đáp án D"",
    ""CorrectOption"": ""A"", 
    ""Explanation"": ""Giải thích ngắn gọn tại sao A đúng.""
  }}
]
(Chú ý: CorrectOption chỉ nhận 1 trong 4 giá trị: A, B, C, D)

NỘI DUNG TÀI LIỆU:
{documentContent}
";
            
            // Quiz dùng model mặc định flash để đảm bảo tính ổn định
            var (responseJson, _) = await _aiService.GenerateContentAsync(prompt, "gemini-2.5-flash");
            if (string.IsNullOrWhiteSpace(responseJson))
                throw new Exception("Không nhận được phản hồi từ AI.");

            // Làm sạch JSON nếu AI vẫn cố tình trả về markdown
            responseJson = responseJson.Trim();
            if (responseJson.StartsWith("```json"))
            {
                responseJson = responseJson.Substring(7);
                if (responseJson.EndsWith("```"))
                {
                    responseJson = responseJson.Substring(0, responseJson.Length - 3);
                }
            }
            responseJson = responseJson.Trim();

            List<QuizQuestionDto> parsedQuestions;
            try
            {
                parsedQuestions = JsonSerializer.Deserialize<List<QuizQuestionDto>>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi parse JSON từ AI: " + ex.Message + "\nJSON gốc: " + responseJson);
            }

            var doc = await _context.Documents.FindAsync(documentId);
            
            var quiz = new Quiz
            {
                DocumentId = documentId,
                Title = $"Bài tập: {doc?.FileName ?? "Tài liệu"}",
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                Questions = parsedQuestions.Select(q => new QuizQuestion
                {
                    Content = q.Question ?? q.Content,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption?.ToUpper() ?? "A",
                    Explanation = q.Explanation
                }).ToList()
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return await GetQuizByIdAsync(quiz.Id);
        }

        public async Task<QuizDto> GetQuizByIdAsync(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return null;

            return new QuizDto
            {
                Id = quiz.Id,
                DocumentId = quiz.DocumentId,
                Title = quiz.Title,
                CreatedAt = quiz.CreatedAt,
                CreatedById = quiz.CreatedById,
                Questions = quiz.Questions.Select(q => new QuizQuestionDto
                {
                    Id = q.Id,
                    QuizId = q.QuizId,
                    Content = q.Content,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption,
                    Explanation = q.Explanation
                }).ToList()
            };
        }

        public async Task<List<QuizDto>> GetQuizzesByDocumentAsync(Guid documentId)
        {
            var quizzes = await _context.Quizzes
                .Where(q => q.DocumentId == documentId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return quizzes.Select(q => new QuizDto
            {
                Id = q.Id,
                DocumentId = q.DocumentId,
                Title = q.Title,
                CreatedAt = q.CreatedAt,
                CreatedById = q.CreatedById
            }).ToList();
        }

        public async Task<QuizResultDto> SubmitQuizAsync(int quizId, int userId, Dictionary<int, string> answers)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);
                
            if (quiz == null) throw new Exception("Không tìm thấy bài Quiz.");

            int score = 0;
            foreach (var q in quiz.Questions)
            {
                if (answers.TryGetValue(q.Id, out string userAnswer) && 
                    userAnswer.Equals(q.CorrectOption, StringComparison.OrdinalIgnoreCase))
                {
                    score++;
                }
            }

            var result = new QuizResult
            {
                QuizId = quizId,
                UserId = userId,
                Score = score,
                TotalQuestions = quiz.Questions.Count,
                CompletedAt = DateTime.UtcNow
            };

            _context.QuizResults.Add(result);
            await _context.SaveChangesAsync();

            return new QuizResultDto
            {
                Id = result.Id,
                QuizId = result.QuizId,
                UserId = result.UserId,
                Score = result.Score,
                TotalQuestions = result.TotalQuestions,
                CompletedAt = result.CompletedAt
            };
        }

        public async Task<List<QuizResultDto>> GetUserQuizResultsAsync(int userId, Guid? documentId = null)
        {
            var query = _context.QuizResults.AsQueryable();
            
            if (documentId.HasValue)
            {
                query = query.Include(r => r.Quiz).Where(r => r.Quiz.DocumentId == documentId.Value);
            }

            var results = await query
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();

            return results.Select(r => new QuizResultDto
            {
                Id = r.Id,
                QuizId = r.QuizId,
                UserId = r.UserId,
                Score = r.Score,
                TotalQuestions = r.TotalQuestions,
                CompletedAt = r.CompletedAt
            }).ToList();
        }
    }
}
