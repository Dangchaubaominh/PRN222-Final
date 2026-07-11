using RagChatbot.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface IBenchmarkRepository
    {
        Task<BenchmarkRun> CreateRunAsync(int adminId, Guid? subjectId);
        Task AddResultAsync(BenchmarkResult result);
        Task<List<BenchmarkRun>> GetAllRunsAsync();
        Task<BenchmarkRun?> GetRunByIdAsync(int id);
    }
}
