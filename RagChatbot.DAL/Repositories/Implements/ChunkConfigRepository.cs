using Microsoft.EntityFrameworkCore;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class ChunkConfigRepository : IChunkConfigRepository
    {
        private readonly ApplicationDbContext _context;

        public ChunkConfigRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SubjectChunkConfig?> GetBySubjectIdAsync(Guid subjectId)
        {
            return await _context.SubjectChunkConfigs.FirstOrDefaultAsync(c => c.SubjectId == subjectId);
        }

        public async Task AddAsync(SubjectChunkConfig config)
        {
            _context.SubjectChunkConfigs.Add(config);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SubjectChunkConfig config)
        {
            _context.SubjectChunkConfigs.Update(config);
            await _context.SaveChangesAsync();
        }
    }
}
