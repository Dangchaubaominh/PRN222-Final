using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.DAL.Entities;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface IDocumentChunkRepository
    {
        IEnumerable<DocumentChunk> GetByDocumentId(Guid documentId);
        int CountByDocumentId(Guid documentId);

        // Tìm các chunk gần nhất với vector câu hỏi trong phạm vi 1 môn học
        // (kèm Document để lấy nguồn trích dẫn)
        Task<IEnumerable<DocumentChunk>> SearchSimilarChunksAsync(Guid subjectId, float[] queryVector, int currentUserId, string currentUserRole, int topK = 3);
    }
}
