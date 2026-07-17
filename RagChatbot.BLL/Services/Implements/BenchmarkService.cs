using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq;
using System.Threading.Tasks;
using RagChatbot.BLL.DTOs;
using System.Text.Json;

namespace RagChatbot.BLL.Services.Implements
{
    public class BenchmarkService : IBenchmarkService
    {
        private readonly IBenchmarkRepository _repository;
        private readonly IAIService _aiService;
        private readonly IDocumentChunkRepository _chunkRepository;

        public BenchmarkService(IBenchmarkRepository repository, IAIService aiService, IDocumentChunkRepository chunkRepository)
        {
            _repository = repository;
            _aiService = aiService;
            _chunkRepository = chunkRepository;
        }

        public async Task<BenchmarkRun> RunAsync(int adminId, Guid? subjectId, List<(string Question, string GroundTruth)> questions, List<string> models)
        {
            var run = await _repository.CreateRunAsync(adminId, subjectId);

            foreach (var model in models)
            {
                foreach (var question in questions)
                {
                    var result = new BenchmarkResult
                    {
                        RunId = run.Id,
                        Model = model,
                        Question = question.Question,
                        GroundTruth = question.GroundTruth
                    };

                    var sw = Stopwatch.StartNew();
                    try
                    {
                        string finalPrompt = question.Question;
                        string contextRetrieved = "";
                        
                        // 1. Thực hiện Retrieval nếu có SubjectId
                        if (subjectId.HasValue)
                        {
                            try
                            {
                                float[] questionVector = await _aiService.GenerateEmbeddingAsync(question.Question);
                                var chunks = await _chunkRepository.SearchSimilarChunksAsync(subjectId.Value, questionVector, adminId, "Admin", topK: 3);
                                
                                if (chunks.Any())
                                {
                                    contextRetrieved = string.Join("\n\n---\n\n", chunks.Select((chunk, index) =>
                                        $"[Nguồn {index + 1}: file \"{chunk.Document?.FileName ?? "Unknown"}\", đoạn {chunk.ChunkIndex ?? index + 1}]\n{chunk.TextContent}"));

                                    finalPrompt = $"""
                                    TÀI LIỆU CUNG CẤP:
                                    {contextRetrieved}

                                    CÂU HỎI CỦA NGƯỜI DÙNG:
                                    {question.Question}

                                    YÊU CẦU TRẢ LỜI:
                                    - Trả lời dựa trên tài liệu được cung cấp.
                                    - Khi nêu thông tin lấy từ tài liệu, có thể ghi số nguồn dạng [Nguồn 1].
                                    - Không tự tạo thông tin.
                                    """;
                                    
                                    result.ContextRetrieved = contextRetrieved;
                                }
                            }
                            catch (Exception ex)
                            {
                                finalPrompt = $"[Lỗi tìm kiếm tài liệu: {ex.Message}]\n\n{question.Question}";
                            }
                        }

                        string answer = "";
                        TokenUsage? tokenUsage = null;
                        
                        // 2. Generation 
                        await foreach (var chunk in _aiService.GenerateChatResponseStreamAsync(finalPrompt, model, usage => tokenUsage = usage))
                        {
                            answer += chunk;
                        }

                        sw.Stop();
                        result.LatencyMs = (int)sw.ElapsedMilliseconds;
                        result.Answer = answer;
                        
                        if (tokenUsage != null)
                        {
                            result.TotalTokens = tokenUsage.TotalTokens;
                            decimal costPerMillion = model.Contains("pro") ? 5.0m : 0.5m;
                            result.EstimatedCost = (result.TotalTokens / 1_000_000m) * costPerMillion;
                        }

                        // 3. RAGAS Evaluation (LLM-as-a-judge)
                        if (!string.IsNullOrWhiteSpace(question.GroundTruth))
                        {
                            try
                            {
                                string evalPrompt = $$"""
                                Bạn là một chuyên gia đánh giá hệ thống RAG (Chatbot hỏi đáp tài liệu).
                                Hãy đánh giá câu trả lời của AI dựa trên các tiêu chí sau và cho điểm từ 0.0 đến 1.0.

                                [ĐẦU VÀO]
                                - Câu hỏi (Question): {{question.Question}}
                                - Đáp án chuẩn (Ground Truth): {{question.GroundTruth}}
                                - Tài liệu tìm được (Context): {{(string.IsNullOrEmpty(contextRetrieved) ? "Không có" : contextRetrieved)}}
                                - Câu trả lời của AI (Answer): {{answer}}

                                [TIÊU CHÍ]
                                1. Faithfulness: Câu trả lời có hoàn toàn dựa trên Context không? (Có bịa thông tin không?)
                                2. AnswerRelevancy: Câu trả lời có giải quyết trực tiếp Câu hỏi không?
                                3. ContextPrecision: Context tìm được có chứa thông tin hữu ích để trả lời không?
                                4. ContextRecall: Context tìm được có chứa ĐẦY ĐỦ thông tin cần thiết so với Đáp án chuẩn không?

                                CHỈ TRẢ VỀ DUY NHẤT một chuỗi JSON hợp lệ với định dạng sau (không có markdown code block, chỉ JSON thuần túy):
                                {
                                    "faithfulness": 0.0,
                                    "answerRelevancy": 0.0,
                                    "contextPrecision": 0.0,
                                    "contextRecall": 0.0
                                }
                                """;

                                var (evalContent, evalUsage) = await _aiService.GenerateContentAsync(evalPrompt, "gemini-2.5-flash-lite");
                                
                                // Parse JSON
                                string cleanJson = evalContent.Replace("```json", "").Replace("```", "").Trim();
                                using var doc = JsonDocument.Parse(cleanJson);
                                var root = doc.RootElement;
                                
                                result.Faithfulness = root.TryGetProperty("faithfulness", out var f) ? f.GetDecimal() : 0;
                                result.AnswerRelevancy = root.TryGetProperty("answerRelevancy", out var a) ? a.GetDecimal() : 0;
                                result.ContextPrecision = root.TryGetProperty("contextPrecision", out var p) ? p.GetDecimal() : 0;
                                result.ContextRecall = root.TryGetProperty("contextRecall", out var r) ? r.GetDecimal() : 0;
                                
                                // Tính thêm cost của phần eval (gemini-2.5-flash-lite = 0.5$ / 1M token)
                                result.TotalTokens += evalUsage.TotalTokens;
                                result.EstimatedCost += (evalUsage.TotalTokens / 1_000_000m) * 0.5m;
                            }
                            catch
                            {
                                // Lỗi đánh giá thì bỏ qua
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        result.LatencyMs = (int)sw.ElapsedMilliseconds;
                        result.Answer = "Error: " + ex.Message;
                    }
                    
                    await _repository.AddResultAsync(result);
                }
            }

            return await _repository.GetRunByIdAsync(run.Id) ?? run;
        }

        public async Task<List<BenchmarkRun>> GetAllRunsAsync()
        {
            return await _repository.GetAllRunsAsync();
        }

        public async Task<BenchmarkRun?> GetRunByIdAsync(int id)
        {
            return await _repository.GetRunByIdAsync(id);
        }
    }
}
