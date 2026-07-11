using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Implements
{
    public class BenchmarkService : IBenchmarkService
    {
        private readonly IBenchmarkRepository _repository;
        private readonly IAIService _aiService;

        public BenchmarkService(IBenchmarkRepository repository, IAIService aiService)
        {
            _repository = repository;
            _aiService = aiService;
        }

        public async Task<BenchmarkRun> RunAsync(int adminId, Guid? subjectId, List<string> questions, List<string> models)
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
                        Question = question
                    };

                    var sw = Stopwatch.StartNew();
                    try
                    {
                        string answer = "";
                        TokenUsage? tokenUsage = null;
                        
                        await foreach (var chunk in _aiService.GenerateChatResponseStreamAsync(question, model, usage => tokenUsage = usage))
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
