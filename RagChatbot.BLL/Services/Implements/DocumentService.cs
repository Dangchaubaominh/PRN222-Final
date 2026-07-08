using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _chunkRepository;

        public DocumentService(IDocumentRepository documentRepository, IDocumentChunkRepository chunkRepository)
        {
            _documentRepository = documentRepository;
            _chunkRepository = chunkRepository;
        }

        public IEnumerable<DocumentDto> GetDocumentsBySubject(Guid subjectId, int currentUserId, string currentUserRole)
        {
            var query = _documentRepository.GetDocumentsBySubjectId(subjectId);
            
            if (currentUserRole != "Admin")
            {
                query = query.Where(d => 
                    d.AccessLevel == DocumentAccessLevel.Public ||
                    (d.AccessLevel == DocumentAccessLevel.Private && d.UploadedById == currentUserId) ||
                    (d.AccessLevel == DocumentAccessLevel.AdminAndLecturerOnly && currentUserRole == "Lecturer")
                );
            }
            
            return query.Select(ToDto);
        }

        public DocumentDto GetDocumentById(Guid id)
        {
            var entity = _documentRepository.GetById(id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<DocumentUploadResult> UploadDocumentAsync(Guid subjectId, string fileName, Stream fileStream, string uploadPath, int uploaderId, int accessLevel)
        {
            if (_documentRepository.ExistsByFileName(subjectId, fileName))
                return DocumentUploadResult.Duplicate;

            try
            {
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                string physicalFilePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(physicalFilePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(stream);
                }

                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    SubjectId = subjectId,
                    FileName = fileName,
                    FilePath = "/uploads/" + uniqueFileName,
                    Status = DocumentStatus.Pending,
                    UploadedById = uploaderId,
                    AccessLevel = (DocumentAccessLevel)accessLevel,
                    UploadedAt = DateTime.UtcNow
                };

                _documentRepository.Add(document);
                return DocumentUploadResult.Success;
            }
            catch
            {
                return DocumentUploadResult.Error;
            }
        }

        public int CountAllDocuments() => _documentRepository.CountAll();

        public bool DeleteDocument(Guid id, string rootPath)
        {
            var document = _documentRepository.GetById(id);
            if (document == null) return false;

            string physicalPath = Path.Combine(rootPath, document.FilePath.TrimStart('/'));
            if (File.Exists(physicalPath))
                File.Delete(physicalPath);

            _documentRepository.Delete(id);
            return true;
        }

        public IEnumerable<DocumentChunkDto> GetChunksByDocumentId(Guid documentId)
        {
            return _chunkRepository.GetByDocumentId(documentId)
                .Select((chunk, index) => new DocumentChunkDto
                {
                    Id          = chunk.Id,
                    Index       = chunk.ChunkIndex ?? index + 1,
                    PageNumber  = chunk.PageNumber,
                    TextContent = chunk.TextContent,
                    WordCount   = chunk.TextContent?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0
                });
        }

    private static DocumentDto ToDto(Document d) => new DocumentDto
        {
            Id = d.Id,
            SubjectId = d.SubjectId,
            FileName = d.FileName,
            FilePath = d.FilePath,
            Status = d.Status.ToString(),
            ProgressMessage = d.ProgressMessage,
            AccessLevel = (int)d.AccessLevel,
            UploadedById = d.UploadedById,
            UploadedAt = d.UploadedAt
        };
    }
}
