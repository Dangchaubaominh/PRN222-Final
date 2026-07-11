using RagChatbot.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IBenchmarkService
    {
        Task<BenchmarkRun> RunAsync(int adminId, Guid? subjectId, List<string> questions, List<string> models);
        Task<List<BenchmarkRun>> GetAllRunsAsync();
        Task<BenchmarkRun?> GetRunByIdAsync(int id);
    }
}
