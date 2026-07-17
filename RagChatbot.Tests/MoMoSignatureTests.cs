using RagChatbot.BLL.Services.Implements;
using Xunit;

namespace RagChatbot.Tests;

public class MoMoSignatureTests
{
    [Fact]
    public void HmacSha256_IsDeterministic_And_KeyDependent()
    {
        var a = MoMoService.HmacSha256("secret", "data");
        var b = MoMoService.HmacSha256("secret", "data");
        var c = MoMoService.HmacSha256("other", "data");

        Assert.Equal(a, b);             // cùng key+data -> cùng chữ ký
        Assert.NotEqual(a, c);          // khác key -> khác chữ ký
        Assert.Equal(64, a.Length);     // SHA-256 = 32 byte = 64 hex
    }
}
