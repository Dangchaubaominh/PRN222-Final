using System.Collections.Generic;
using RagChatbot.BLL.Services.Implements;
using Xunit;

namespace RagChatbot.Tests;

public class VnPaySignerTests
{
    [Fact]
    public void BuildQuery_SortsByKey_And_UrlEncodesValues()
    {
        // SortedDictionary(Ordinal) giữ key theo thứ tự; giá trị phải được URL-encode (space -> +)
        var data = new SortedDictionary<string, string>(System.StringComparer.Ordinal)
        {
            ["vnp_OrderInfo"] = "Thanh toan goi Pro",
            ["vnp_Amount"] = "9900000"
        };

        var query = VnPayService.BuildQuery(data);

        // vnp_Amount đứng trước vnp_OrderInfo (ordinal), khoảng trắng thành '+'
        Assert.Equal("vnp_Amount=9900000&vnp_OrderInfo=Thanh+toan+goi+Pro", query);
    }
}
