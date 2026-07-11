using Microsoft.EntityFrameworkCore;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class BenchmarkRepository : IBenchmarkRepository
    {
        private readonly ApplicationDbContext _context;

        public BenchmarkRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BenchmarkRun> CreateRunAsync(int adminId, Guid? subjectId)
        {
            var run = new BenchmarkRun
            {
                CreatedById = adminId,
                SubjectId = subjectId,
                CreatedAt = DateTime.UtcNow
            };
            _context.BenchmarkRuns.Add(run);
            await _context.SaveChangesAsync();
            return run;
        }

        public async Task AddResultAsync(BenchmarkResult result)
        {
            _context.BenchmarkResults.Add(result);
            await _context.SaveChangesAsync();
        }

        public async Task<List<BenchmarkRun>> GetAllRunsAsync()
        {
            return await _context.BenchmarkRuns
                .Include(r => r.CreatedBy)
                .Include(r => r.Subject)
                .Include(r => r.Results)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<BenchmarkRun?> GetRunByIdAsync(int id)
        {
            return await _context.BenchmarkRuns
                .Include(r => r.CreatedBy)
                .Include(r => r.Subject)
                .Include(r => r.Results)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}
