using RagChatbot.BLL.Services.Implements;
using RagChatbot.DAL.Entities;
using Xunit;

namespace RagChatbot.Tests;

/// <summary>
/// Xác nhận fix: chunk không còn bị "hụt" giữa chừng vì ranh giới trang PDF.
/// Trước fix, mỗi trang được chia riêng (SelectMany theo segment) nên trang có
/// số từ không phải bội số của MaxWordsPerChunk sẽ luôn để lại 1 chunk ngắn hụt.
/// </summary>
public class DocumentChunkingPipelineTests
{
    private static string WordsText(int count, string prefix = "tu")
        => string.Join(" ", Enumerable.Range(1, count).Select(i => $"{prefix}{i}"));

    [Fact]
    public void FixedStrategy_AcrossPageBoundary_NoUndersizedChunkInMiddle()
    {
        // Mô phỏng đúng tình huống thực tế: trang 20 có 439 từ, trang 21 có 500 từ.
        // Trước fix: chunk theo trang 20 -> 400 + 39 (hụt), rồi trang 21 lại bắt đầu từ 0 -> 400 + 100.
        // Sau fix: gộp 939 từ liên tục -> 400 + 400 + 139, chỉ chunk CUỐI CÙNG được phép ngắn hơn.
        var segments = new List<DocumentProcessingService.TextSegment>
        {
            new(WordsText(439), 20),
            new(WordsText(500, "b"), 21)
        };

        var config = new SubjectChunkConfig { MaxWordsPerChunk = 400, OverlapSentences = 0, Strategy = "Fixed" };

        var (fullText, pageWordBoundaries, pageNumbers) = DocumentProcessingService.MergeSegments(segments);
        var chunkTexts = DocumentProcessingService.SplitByStrategy(fullText, config);

        Assert.Equal(3, chunkTexts.Count);
        for (int i = 0; i < chunkTexts.Count - 1; i++)
            Assert.Equal(400, DocumentProcessingService.CountWords(chunkTexts[i]));

        // Chunk cuối = 939 - 400*2 = 139, không tránh được (tổng không chia hết cho 400)
        Assert.Equal(139, DocumentProcessingService.CountWords(chunkTexts[^1]));
    }

    [Fact]
    public void PageForWordIndex_MapsChunkToCorrectSourcePage()
    {
        var segments = new List<DocumentProcessingService.TextSegment>
        {
            new(WordsText(439), 20),
            new(WordsText(500, "b"), 21)
        };

        var (_, pageWordBoundaries, pageNumbers) = DocumentProcessingService.MergeSegments(segments);

        // Từ thứ 0..438 thuộc trang 20, từ thứ 439 trở đi thuộc trang 21
        Assert.Equal(20, DocumentProcessingService.PageForWordIndex(0, pageWordBoundaries, pageNumbers));
        Assert.Equal(20, DocumentProcessingService.PageForWordIndex(438, pageWordBoundaries, pageNumbers));
        Assert.Equal(21, DocumentProcessingService.PageForWordIndex(439, pageWordBoundaries, pageNumbers));
        Assert.Equal(21, DocumentProcessingService.PageForWordIndex(900, pageWordBoundaries, pageNumbers));
    }
}
