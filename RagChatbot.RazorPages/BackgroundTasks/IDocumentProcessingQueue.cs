namespace RagChatbot.RazorPages.BackgroundTasks
{
    /// <summary>
    /// Hàng đợi nền chứa Id các tài liệu cần AI xử lý (chunk + embedding).
    /// Trang upload chỉ cần Enqueue rồi trả về ngay; worker sẽ xử lý lần lượt.
    /// </summary>
    public interface IDocumentProcessingQueue
    {
        void Enqueue(Guid documentId);
        IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
    }
}
