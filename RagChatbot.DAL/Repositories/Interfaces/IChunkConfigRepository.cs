using RagChatbot.DAL.Entities;
using System;
using System.Threading.Tasks;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface IChunkConfigRepository
    {
        Task<SubjectChunkConfig?> GetBySubjectIdAsync(Guid subjectId);
        Task UpdateAsync(SubjectChunkConfig config);
        Task AddAsync(SubjectChunkConfig config);
    }
}
