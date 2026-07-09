using System.Threading.Channels;

namespace RagChatbot.RazorPages.BackgroundTasks
{
    public class DocumentProcessingQueue : IDocumentProcessingQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

        public void Enqueue(Guid documentId) => _channel.Writer.TryWrite(documentId);

        public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
