using RagChatbot.DAL.Entities;
using System;
using System.Threading.Tasks;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface ITokenUsageRepository
    {
        /// <summary>Thêm một bản ghi log token mới vào DB</summary>
        Task AddAsync(TokenUsageLog log);

        /// <summary>
        /// Tổng số token đã dùng của một user trong khoảng [from, to].
        /// Dùng bởi ITokenUsageService.TotalTokens() và Vũ (Thống kê).
        /// </summary>
        Task<long> SumTokensAsync(int userId, DateTime from, DateTime to);
    }
}
