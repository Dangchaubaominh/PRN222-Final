using RagChatbot.BLL.Helpers;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Pgvector;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Tesseract;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RagChatbot.Tests")]

namespace RagChatbot.BLL.Services.Implements
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly IDocumentRepository _documentRepo;
        private readonly IAIService _aiService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IChunkConfigService _chunkConfigService;

        public DocumentProcessingService(IDocumentRepository documentRepo, IAIService aiService, ApplicationDbContext context, IConfiguration config, IChunkConfigService chunkConfigService)
        {
            _documentRepo = documentRepo;
            _aiService = aiService;
            _context = context;
            _config = config;
            _chunkConfigService = chunkConfigService;
        }

        public async Task<bool> ProcessDocumentAsync(Guid documentId, string rootPath, Action<string>? onProgress = null)
        {
            // 1. Lấy thông tin tài liệu từ DB
            var doc = _documentRepo.GetById(documentId);
            if (doc == null) return false;

            string physicalPath = Path.Combine(rootPath, doc.FilePath.TrimStart('/'));
            if (!File.Exists(physicalPath)) return false;

            // 2. Đọc nội dung chữ và giữ lại metadata nguồn nếu định dạng hỗ trợ
            List<TextSegment> textSegments = new();
            var extension = Path.GetExtension(doc.FileName).ToLower();

            try
            {
                if (extension == ".txt")
                {
                    onProgress?.Invoke("Đang đọc nội dung text...");
                    textSegments.Add(new TextSegment(await File.ReadAllTextAsync(physicalPath), null));
                }
                else if (extension == ".pdf")
                {
                    onProgress?.Invoke("Đang đọc nội dung PDF...");
                    textSegments.AddRange(ExtractTextFromPdf(physicalPath));
                }
                else if (extension == ".docx" || extension == ".doc")
                {
                    onProgress?.Invoke("Đang đọc nội dung Word...");
                    textSegments.Add(new TextSegment(ExtractTextFromDocx(physicalPath), null));
                }
                else
                {
                    return false; // Định dạng chưa hỗ trợ
                }

                // 2b. PDF không có lớp chữ (scan/ảnh) → thử OCR bằng Tesseract
                if (!textSegments.Any(segment => !string.IsNullOrWhiteSpace(segment.Text)) && extension == ".pdf")
                {
                    onProgress?.Invoke("Đang chạy nhận dạng ký tự (OCR)...");
                    textSegments.Add(new TextSegment(OcrPdf(physicalPath), null));
                }

                // 2c. Vẫn không trích được chữ (file rỗng, mã hoá, OCR không khả dụng) → Failed
                if (!textSegments.Any(segment => !string.IsNullOrWhiteSpace(segment.Text)))
                {
                    _documentRepo.UpdateStatus(documentId, DocumentStatus.Failed);
                    return false;
                }

                // 3. Chunking theo Strategy cấu hình cho môn học:
                //    - "Semantic": chia theo ranh giới đoạn văn/câu (không cắt giữa câu, kích thước không đều)
                //    - "Fixed": cắt cứng đúng N từ mỗi chunk (kích thước đều tuyệt đối theo MaxWordsPerChunk)
                //    Ghép toàn bộ trang thành 1 luồng văn bản liên tục trước khi chia — nếu chia riêng
                //    từng trang, trang nào ít chữ hơn MaxWordsPerChunk sẽ luôn ra 1 chunk ngắn hụt,
                //    kể cả ở chế độ Fixed (không thể "mượn" thêm chữ từ trang kế tiếp để đủ N từ).
                onProgress?.Invoke("Đang chia nhỏ văn bản (Chunking)...");
                var chunkConfig = await _chunkConfigService.GetForSubjectAsync(doc.SubjectId);

                var validSegments = textSegments.Where(segment => !string.IsNullOrWhiteSpace(segment.Text)).ToList();
                var (fullText, pageWordBoundaries, pageNumbers) = MergeSegments(validSegments);

                int wordCursor = 0;
                var chunks = SplitByStrategy(fullText, chunkConfig)
                    .Select(chunkText =>
                    {
                        var segment = new TextSegment(chunkText, PageForWordIndex(wordCursor, pageWordBoundaries, pageNumbers));
                        wordCursor += CountWords(chunkText);
                        return segment;
                    })
                    .ToList();

                // 4. Gọi AI chuyển từng chunk thành Vector embedding 768 chiều
                doc.Status = DocumentStatus.Processing;
                await _context.SaveChangesAsync();

                int chunkIndex = 1;
                foreach (var chunk in chunks)
                {
                    onProgress?.Invoke($"Đang nhúng Vector ({chunkIndex}/{chunks.Count})...");
                    float[] vectorArray = await _aiService.GenerateEmbeddingAsync(chunk.Text);
                    var docChunk = new DocumentChunk
                    {
                        Id          = Guid.NewGuid(),
                        DocumentId  = documentId,
                        TextContent = chunk.Text,
                        ChunkIndex  = chunkIndex++,
                        PageNumber  = chunk.PageNumber,
                        Embedding   = new Vector(vectorArray)
                    };
                    _context.DocumentChunks.Add(docChunk);
                }

                doc.Status = DocumentStatus.Completed;
                doc.ProgressMessage = "Hoàn tất";
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                // Hủy mọi thay đổi đang chờ (các chunk thêm dở dang chưa lưu),
                // rồi chỉ đánh dấu tài liệu là Failed.
                _context.ChangeTracker.Clear();
                var failedDoc = _documentRepo.GetById(documentId);
                if (failedDoc != null)
                {
                    failedDoc.Status = DocumentStatus.Failed;
                    failedDoc.ProgressMessage = "Lỗi xử lý";
                    _context.SaveChanges();
                }
                return false;
            }
        }

        internal static List<string> SplitByStrategy(string text, SubjectChunkConfig cfg)
        {
            return string.Equals(cfg.Strategy, "Fixed", StringComparison.OrdinalIgnoreCase)
                ? FixedChunker.SplitText(text, cfg.MaxWordsPerChunk, overlapWords: cfg.OverlapSentences)
                : SemanticChunker.SplitText(text, cfg.MaxWordsPerChunk, overlapSentences: cfg.OverlapSentences);
        }

        /// <summary>
        /// Ghép text các trang thành 1 chuỗi liên tục (giữ nguyên xuống dòng nội bộ mỗi trang
        /// để SemanticChunker vẫn nhận diện được ranh giới đoạn văn), đồng thời trả về mốc số từ
        /// tích luỹ cuối mỗi trang — dùng để suy ra trang gần đúng của mỗi chunk sau khi chia.
        /// </summary>
        internal static (string FullText, int[] PageWordBoundaries, int?[] PageNumbers) MergeSegments(List<TextSegment> segments)
        {
            var sb = new StringBuilder();
            var boundaries = new int[segments.Count];
            var pages = new int?[segments.Count];
            int running = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(segments[i].Text);

                running += CountWords(segments[i].Text);
                boundaries[i] = running;
                pages[i] = segments[i].PageNumber;
            }

            return (sb.ToString(), boundaries, pages);
        }

        /// <summary>Trang chứa từ thứ <paramref name="wordIndex"/> (0-based) trong văn bản đã ghép.</summary>
        internal static int? PageForWordIndex(int wordIndex, int[] pageWordBoundaries, int?[] pageNumbers)
        {
            for (int i = 0; i < pageWordBoundaries.Length; i++)
                if (wordIndex < pageWordBoundaries[i]) return pageNumbers[i];

            return pageNumbers.Length > 0 ? pageNumbers[^1] : null;
        }

        internal static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private List<TextSegment> ExtractTextFromPdf(string filePath)
        {
            var segments = new List<TextSegment>();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    // ContentOrderTextExtractor sắp xếp chữ theo đúng thứ tự đọc
                    // (tốt hơn page.Text cho PDF nhiều cột / bố cục phức tạp).
                    string pageText;
                    try { pageText = ContentOrderTextExtractor.GetText(page); }
                    catch { pageText = page.Text; }

                    if (string.IsNullOrWhiteSpace(pageText))
                        pageText = page.Text; // fallback

                    if (!string.IsNullOrWhiteSpace(pageText))
                        segments.Add(new TextSegment(pageText, page.Number));
                }
            }
            return segments;
        }

        // OCR cho PDF scan/ảnh: render từng trang thành ảnh rồi nhận dạng bằng Tesseract.
        // Best-effort: nếu thiếu native/tessdata hoặc lỗi → trả rỗng (tài liệu sẽ bị Failed).
        private string OcrPdf(string filePath)
        {
            try
            {
                string tessPath = _config["Ocr:TessDataPath"];
                if (string.IsNullOrWhiteSpace(tessPath))
                    tessPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
                if (!Directory.Exists(tessPath))
                    return string.Empty;

                string langs = _config["Ocr:Languages"];
                if (string.IsNullOrWhiteSpace(langs))
                    langs = "vie+eng";

                byte[] pdfBytes = File.ReadAllBytes(filePath);
                var sb = new StringBuilder();

                using var engine = new TesseractEngine(tessPath, langs, EngineMode.Default);
                foreach (var bitmap in PDFtoImage.Conversion.ToImages(pdfBytes))
                {
                    using (bitmap)
                    using (var data = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                    using (var pix = Pix.LoadFromMemory(data.ToArray()))
                    using (var page = engine.Process(pix))
                    {
                        sb.AppendLine(page.GetText());
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private string ExtractTextFromDocx(string filePath)
        {
            var sb = new StringBuilder();
            using var wordDoc = WordprocessingDocument.Open(filePath, isEditable: false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            foreach (var para in body.Descendants<Paragraph>())
            {
                string line = para.InnerText.Trim();
                if (line.Length > 0)
                    sb.AppendLine(line);
            }
            return sb.ToString();
        }

        internal sealed record TextSegment(string Text, int? PageNumber);
    }
}
