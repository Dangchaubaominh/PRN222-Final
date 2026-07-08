using RagChatbot.BLL.Helpers;
using Xunit;

namespace RagChatbot.Tests;

public class SemanticChunkerTests
{
    [Fact]
    public void SplitText_EmptyInput_ReturnsEmptyList()
    {
        var result = SemanticChunker.SplitText("", maxWordsPerChunk: 400, overlapSentences: 2);
        Assert.Empty(result);
    }

    [Fact]
    public void SplitText_ShortParagraph_ReturnsSingleChunk()
    {
        var text = "Câu một. Câu hai. Câu ba.";
        var result = SemanticChunker.SplitText(text, maxWordsPerChunk: 400, overlapSentences: 2);
        Assert.Single(result);
        Assert.Contains("Câu một", result[0]);
    }

    [Fact]
    public void SplitText_ExceedsMaxWords_SplitsIntoMultipleChunks()
    {
        // 50 câu, mỗi câu ~10 từ, maxWords nhỏ để ép chia
        var sentences = string.Join(" ", Enumerable.Range(1, 50)
            .Select(i => $"Đây là câu số {i} với một ít nội dung phụ."));
        var result = SemanticChunker.SplitText(sentences, maxWordsPerChunk: 40, overlapSentences: 2);
        Assert.True(result.Count > 1, "Văn bản dài phải được chia thành nhiều chunk");
    }
}
