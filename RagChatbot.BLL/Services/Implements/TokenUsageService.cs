using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Implements
{
    public class TokenUsageService : ITokenUsageService
    {
        private readonly ITokenUsageRepository _repo;

        public TokenUsageService(ITokenUsageRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Ghi log đồng bộ bằng async/await. SignalR sẽ chờ việc lưu DB hoàn tất, 
        /// đảm bảo DbContext không bị dispose sớm.
        /// </summary>
        public async Task LogAsync(int userId, Guid? subjectId, string model, int promptTokens, int completionTokens)
        {
            var log = new TokenUsageLog
            {
                UserId            = userId,
                SubjectId         = subjectId,
                Model             = model ?? string.Empty,
                PromptTokens      = promptTokens,
                CompletionTokens  = completionTokens,
                TotalTokens       = promptTokens + completionTokens,
                CreatedAt         = DateTime.UtcNow
            };

            await _repo.AddAsync(log);
        }

        /// <summary>Tổng token của userId trong [from, to]. Dùng cho Vũ (Thống kê).</summary>
        public Task<long> TotalTokens(int userId, DateTime from, DateTime to)
        {
            return _repo.SumTokensAsync(userId, from, to);
        }
    }
}
