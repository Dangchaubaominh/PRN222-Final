using System;
using System.Collections.Generic;
using System.Linq;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Data;

namespace RagChatbot.BLL.Services.Implements
{
    /// <summary>
    /// Thống kê doanh thu (từ PaymentOrder) và token (từ TokenUsageLog).
    /// Aggregate bằng LINQ trên ApplicationDbContext (cùng cách DocumentProcessingService dùng DbContext).
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _db;

        public StatisticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public AdminStats GetAdminStats(int days = 30)
        {
            var from = DateTime.UtcNow.Date.AddDays(-days + 1);
            var paid = _db.PaymentOrders.Where(o => o.Status == "Paid");

            var stats = new AdminStats
            {
                TotalRevenue = paid.Sum(o => (decimal?)o.Amount) ?? 0,
                PaidOrders = paid.Count(),
                TotalTokens = _db.TokenUsageLogs.Sum(t => (long?)t.TotalTokens) ?? 0
            };

            // Doanh thu theo ngày
            stats.RevenueByDay = paid.Where(o => o.CreatedAt >= from)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(g => g.Day)
                .ToList()
                .Select(g => new DailyRevenue(g.Day.ToString("dd/MM"), g.Amount))
                .ToList();

            // Doanh thu theo gói
            stats.RevenueByPackage = (from o in paid
                                      join p in _db.Packages on o.PackageId equals p.Id
                                      group o by p.Name into g
                                      select new { Package = g.Key, Amount = g.Sum(x => x.Amount), Orders = g.Count() })
                .ToList()
                .Select(g => new RevenueByPackage(g.Package, g.Amount, g.Orders))
                .ToList();

            // Token theo ngày
            stats.TokensByDay = _db.TokenUsageLogs.Where(t => t.CreatedAt >= from)
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Tokens = g.Sum(x => (long)x.TotalTokens) })
                .OrderBy(g => g.Day)
                .ToList()
                .Select(g => new DailyToken(g.Day.ToString("dd/MM"), g.Tokens))
                .ToList();

            // Token theo model
            stats.TokensByModel = _db.TokenUsageLogs
                .GroupBy(t => t.Model)
                .Select(g => new { Model = g.Key, Tokens = g.Sum(x => (long)x.TotalTokens) })
                .ToList()
                .Select(g => new TokenByModel(string.IsNullOrEmpty(g.Model) ? "(khác)" : g.Model, g.Tokens))
                .ToList();

            return stats;
        }

        public LecturerStats GetLecturerStats(int userId)
        {
            var subjectIds = _db.UserSubjects
                .Where(us => us.UserId == userId)
                .Select(us => us.SubjectId)
                .ToList();

            var subjects = _db.Subjects.Where(s => subjectIds.Contains(s.Id)).ToList();

            var result = new LecturerStats();
            foreach (var s in subjects)
            {
                int questions = _db.ChatMessages.Count(m => m.SubjectId == s.Id && m.Sender == "user");
                long tokens = _db.TokenUsageLogs.Where(t => t.SubjectId == s.Id).Sum(t => (long?)t.TotalTokens) ?? 0;
                result.Subjects.Add(new SubjectStat(s.Code, questions, tokens));
            }

            return result;
        }
    }
}
