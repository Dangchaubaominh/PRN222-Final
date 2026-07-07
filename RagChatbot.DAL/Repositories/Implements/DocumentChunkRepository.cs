using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class DocumentChunkRepository : IDocumentChunkRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentChunkRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<DocumentChunk> GetByDocumentId(Guid documentId)
        {
            return _context.DocumentChunks
                           .Where(c => c.DocumentId == documentId)
                           .OrderBy(c => c.ChunkIndex)
                           .ToList();
        }

        public int CountByDocumentId(Guid documentId)
        {
            return _context.DocumentChunks.Count(c => c.DocumentId == documentId);
        }

        public async Task<IEnumerable<DocumentChunk>> SearchSimilarChunksAsync(Guid subjectId, float[] queryVector, int currentUserId, string currentUserRole, int topK = 3)
        {
            var vector = new Vector(queryVector);

            var query = _context.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document.SubjectId == subjectId && c.Document.Status == DocumentStatus.Completed);
                
            if (currentUserRole != "Admin")
            {
                query = query.Where(c => 
                    c.Document.AccessLevel == DocumentAccessLevel.Public ||
                    (c.Document.AccessLevel == DocumentAccessLevel.Private && c.Document.UploadedById == currentUserId) ||
                    (c.Document.AccessLevel == DocumentAccessLevel.AdminAndLecturerOnly && currentUserRole == "Lecturer")
                );
            }

            return await query
                .OrderBy(c => c.Embedding.CosineDistance(vector))
                .Take(topK)
                .ToListAsync();
        }
    }
}
