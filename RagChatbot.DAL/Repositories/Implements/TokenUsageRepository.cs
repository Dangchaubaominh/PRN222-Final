using Microsoft.EntityFrameworkCore;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class TokenUsageRepository : ITokenUsageRepository
    {
        private readonly ApplicationDbContext _context;

        public TokenUsageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Lưu một bản ghi token vào DB (async, không chặn luồng chat)</summary>
        public async Task AddAsync(TokenUsageLog log)
        {
            _context.TokenUsageLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Tổng token của userId trong khoảng [from, to].
        /// Được gọi bởi TokenUsageService.TotalTokens() và Vũ (Dashboard).
        /// </summary>
        public async Task<long> SumTokensAsync(int userId, DateTime from, DateTime to)
        {
            return await _context.TokenUsageLogs
                .Where(t => t.UserId == userId && t.CreatedAt >= from && t.CreatedAt <= to)
                .SumAsync(t => (long)t.TotalTokens);
        }
    }
}
