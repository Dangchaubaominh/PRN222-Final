using RagChatbot.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Interfaces
{
    public enum DocumentUploadResult
    {
        Success,
        Duplicate,
        Error,
        TooLarge,
        TooManyPages
    }

    public interface IDocumentService
    {
        IEnumerable<DocumentDto> GetDocumentsBySubject(Guid subjectId, int currentUserId, string currentUserRole);
        DocumentDto GetDocumentById(Guid id);
        // PdfPageCount chỉ có giá trị khi Result == TooManyPages (số trang thực tế của file vừa chọn)
        Task<(DocumentUploadResult Result, int? PdfPageCount)> UploadDocumentAsync(Guid subjectId, string fileName, Stream fileStream, string uploadPath, int uploaderId, int accessLevel);
        bool DeleteDocument(Guid id, string rootPath);

        // Tổng số tài liệu trong toàn hệ thống (dùng cho dashboard)
        int CountAllDocuments();

        // Lấy danh sách các chunk đã được chia từ tài liệu
        IEnumerable<DocumentChunkDto> GetChunksByDocumentId(Guid documentId);

        // Tìm kiếm semantic: nhúng câu hỏi rồi tìm chunk tương tự nhất (lọc quyền theo role)
        Task<IEnumerable<DocumentChunkDto>> SearchChunksAsync(Guid subjectId, string query, int currentUserId, string currentUserRole, int topK = 10);
    }
}
