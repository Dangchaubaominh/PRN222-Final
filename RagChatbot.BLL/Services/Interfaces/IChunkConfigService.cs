using RagChatbot.DAL.Entities;
using System;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IChunkConfigService
    {
        Task<SubjectChunkConfig> GetForSubjectAsync(Guid subjectId);
        Task SaveAsync(SubjectChunkConfig config);
    }
}
