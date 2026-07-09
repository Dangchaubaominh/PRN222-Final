using Microsoft.AspNetCore.SignalR;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.Hubs;
using RagChatbot.RazorPages.Services;

namespace RagChatbot.RazorPages.BackgroundTasks
{
    /// <summary>
    /// Worker chạy nền: lấy từng tài liệu trong hàng đợi, gọi BLL xử lý RAG,
    /// và đẩy trạng thái (Processing → Completed/Failed) tới group của môn học
    /// qua SignalR để giao diện cập nhật real-time mà không cần F5.
    /// </summary>
    public class DocumentProcessingWorker : BackgroundService
    {
        private readonly IDocumentProcessingQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<DocumentHub> _hub;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentProcessingWorker> _logger;

        public DocumentProcessingWorker(
            IDocumentProcessingQueue queue,
            IServiceScopeFactory scopeFactory,
            IHubContext<DocumentHub> hub,
            IWebHostEnvironment env,
            ILogger<DocumentProcessingWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _hub = hub;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var documentId in _queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessOneAsync(documentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xử lý nền cho tài liệu {DocumentId}", documentId);
                }
            }
        }

        private async Task ProcessOneAsync(Guid documentId)
        {
            // Mỗi tài liệu xử lý trong một DI scope riêng (DbContext/Service là Scoped)
            using var scope = _scopeFactory.CreateScope();
            var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
            var processing = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();

            var doc = documentService.GetDocumentById(documentId);
            if (doc == null) return;

            string group = DocumentHub.GroupName(doc.SubjectId);

            // Báo "đang xử lý"
            await Notify(group, documentId, "Processing");

            bool ok = await processing.ProcessDocumentAsync(documentId, _env.WebRootPath, msg => 
            {
                _hub.Clients.Group(group).SendAsync("DocumentProgressChanged", new { documentId, message = msg });
            });

            // Báo kết quả cuối (cập nhật badge trên trang danh sách)
            await Notify(group, documentId, ok ? "Completed" : "Failed");

            // Khi học xong: thông báo tới mọi thành viên của môn học về tài liệu mới
            if (ok)
            {
                var subjectService = scope.ServiceProvider.GetRequiredService<ISubjectService>();
                var userSubjectService = scope.ServiceProvider.GetRequiredService<IUserSubjectService>();
                var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();

                var subject = subjectService.GetSubjectById(doc.SubjectId);
                string label = subject == null ? "môn học" : $"{subject.Code} — {subject.Name}";
                var memberIds = userSubjectService.GetAssignedUsers(doc.SubjectId).Select(u => u.Id).ToList();

                await notifier.NotifyUsersAsync(
                    memberIds,
                    $"Môn {label} có tài liệu mới đã sẵn sàng: \"{doc.FileName}\".",
                    "info",
                    $"/Document?subjectId={doc.SubjectId}");
            }
            else
            {
                var notifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
                if (doc.UploadedById.HasValue)
                {
                    await notifier.NotifyUserAsync(
                        doc.UploadedById.Value,
                        $"Lỗi xử lý tài liệu \"{doc.FileName}\". Vui lòng kiểm tra lại.",
                        "error",
                        $"/Document?subjectId={doc.SubjectId}");
                }
            }
        }

        private Task Notify(string group, Guid documentId, string status)
            => _hub.Clients.Group(group).SendAsync("DocumentStatusChanged", new
            {
                documentId,
                status
            });
    }
}
