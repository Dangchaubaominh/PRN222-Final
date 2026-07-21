using RagChatbot.BLL.Helpers;
using Xunit;

namespace RagChatbot.Tests;

public class FixedChunkerTests
{
    [Fact]
    public void SplitText_EmptyInput_ReturnsEmptyList()
    {
        var result = FixedChunker.SplitText("", maxWordsPerChunk: 400, overlapWords: 0);
        Assert.Empty(result);
    }

    [Fact]
    public void SplitText_ExactMultiple_AllChunksMatchConfiguredSize()
    {
        // 800 từ, maxWords = 400 -> đúng 2 chunk, mỗi chunk đúng 400 từ
        var text = string.Join(" ", Enumerable.Range(1, 800).Select(i => $"tu{i}"));
        var result = FixedChunker.SplitText(text, maxWordsPerChunk: 400, overlapWords: 0);

        Assert.Equal(2, result.Count);
        foreach (var chunk in result)
            Assert.Equal(400, chunk.Split(' ').Length);
    }

    [Fact]
    public void SplitText_NotExactMultiple_LastChunkIsShorterOnly()
    {
        // 950 từ, maxWords = 400 -> 2 chunk đủ 400, chunk cuối 150 (không chia hết là chuyện bình thường)
        var text = string.Join(" ", Enumerable.Range(1, 950).Select(i => $"tu{i}"));
        var result = FixedChunker.SplitText(text, maxWordsPerChunk: 400, overlapWords: 0);

        Assert.Equal(3, result.Count);
        Assert.Equal(400, result[0].Split(' ').Length);
        Assert.Equal(400, result[1].Split(' ').Length);
        Assert.Equal(150, result[2].Split(' ').Length);
    }

    [Fact]
    public void SplitText_WithOverlap_RepeatsWordsBetweenChunks()
    {
        var text = string.Join(" ", Enumerable.Range(1, 20).Select(i => $"tu{i}"));
        var result = FixedChunker.SplitText(text, maxWordsPerChunk: 10, overlapWords: 3);

        Assert.True(result.Count > 1);
        // 3 từ cuối chunk đầu phải trùng với 3 từ đầu chunk sau
        var tailOfFirst = result[0].Split(' ')[^3..];
        var headOfSecond = result[1].Split(' ')[..3];
        Assert.Equal(tailOfFirst, headOfSecond);
    }
}
