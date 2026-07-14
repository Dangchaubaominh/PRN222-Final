using System.Collections.Generic;
using System.Linq;
using RagChatbot.BLL.Services.Implements;
using RagChatbot.DAL.Entities;
using Xunit;

namespace RagChatbot.Tests.Packages;

public class MoMoServiceTests
{
    private const string Secret = "test-secret";
    private const string Access = "test-access";

    private static (MoMoService svc, FakePaymentRepository pays, SpySubscriptionService subs) Build(PaymentOrder order)
    {
        var pkgs = new FakePackageRepository();
        pkgs.Items.Add(new Package { Id = 2, Name = "Basic", Price = 49000, TokenQuota = 500_000, AllowedModels = "m", DurationDays = 30, IsActive = true });
        var pays = new FakePaymentRepository();
        pays.Items.Add(order);
        var subs = new SpySubscriptionService();
        var cfg = new FakeConfig(new Dictionary<string, string?> { ["Momo:SecretKey"] = Secret, ["Momo:AccessKey"] = Access });
        return (new MoMoService(cfg, pkgs, pays, subs), pays, subs);
    }

    private static PaymentOrder PendingOrder(string txnRef = "ORDER1", decimal amount = 49000, string status = "Pending")
        => new PaymentOrder { UserId = 5, PackageId = 2, Amount = amount, VnpTxnRef = txnRef, Status = status };

    // Dựng callback MoMo có chữ ký hợp lệ (đúng thứ tự field MoMo dùng khi verify)
    private static Dictionary<string, string> SignedCallback(string orderId, string amount, string resultCode)
    {
        var p = new Dictionary<string, string>
        {
            ["partnerCode"] = "MOMO",
            ["orderId"] = orderId,
            ["requestId"] = orderId,
            ["amount"] = amount,
            ["orderInfo"] = "Thanh toan goi",
            ["orderType"] = "momo_wallet",
            ["transId"] = "999888777",
            ["resultCode"] = resultCode,
            ["message"] = "Success",
            ["payType"] = "qr",
            ["responseTime"] = "1700000000000",
            ["extraData"] = ""
        };
        string raw =
            $"accessKey={Access}&amount={p["amount"]}&extraData={p["extraData"]}&message={p["message"]}" +
            $"&orderId={p["orderId"]}&orderInfo={p["orderInfo"]}&orderType={p["orderType"]}" +
            $"&partnerCode={p["partnerCode"]}&payType={p["payType"]}&requestId={p["requestId"]}" +
            $"&responseTime={p["responseTime"]}&resultCode={p["resultCode"]}&transId={p["transId"]}";
        p["signature"] = MoMoService.HmacSha256(Secret, raw);
        return p;
    }

    [Fact]
    public void HandleReturn_MissingSignature_Fails()
    {
        var (svc, _, subs) = Build(PendingOrder());
        var res = svc.HandleReturn(new Dictionary<string, string> { ["orderId"] = "ORDER1" });
        Assert.False(res.Success);
        Assert.Equal(0, subs.ActivateCalls);
    }

    [Fact]
    public void HandleReturn_InvalidSignature_Fails()
    {
        var (svc, pays, subs) = Build(PendingOrder());
        var cb = SignedCallback("ORDER1", "49000", "0");
        cb["signature"] = "deadbeef"; // giả mạo
        var res = svc.HandleReturn(cb);
        Assert.False(res.Success);
        Assert.Equal("Pending", pays.Items.Single().Status); // không đổi
        Assert.Equal(0, subs.ActivateCalls);                 // không kích hoạt
    }

    [Fact]
    public void HandleReturn_ValidSuccess_MarksPaid_And_ActivatesOnce()
    {
        var (svc, pays, subs) = Build(PendingOrder());
        var res = svc.HandleReturn(SignedCallback("ORDER1", "49000", "0"));
        Assert.True(res.Success);
        Assert.Equal("Paid", pays.Items.Single().Status);
        Assert.NotNull(pays.Items.Single().PaidAt);
        Assert.Equal(1, subs.ActivateCalls);
        Assert.Equal((5, 2), subs.LastActivate);
    }

    [Fact]
    public void HandleReturn_AmountMismatch_Fails()
    {
        var (svc, pays, subs) = Build(PendingOrder(amount: 49000));
        var res = svc.HandleReturn(SignedCallback("ORDER1", "10000", "0")); // số tiền bị sửa
        Assert.False(res.Success);
        Assert.Equal("Failed", pays.Items.Single().Status);
        Assert.Equal(0, subs.ActivateCalls);
    }

    [Fact]
    public void HandleReturn_ResultCodeNonZero_MarksFailed()
    {
        var (svc, pays, subs) = Build(PendingOrder());
        var res = svc.HandleReturn(SignedCallback("ORDER1", "49000", "1006")); // user hủy / lỗi
        Assert.False(res.Success);
        Assert.Equal("Failed", pays.Items.Single().Status);
        Assert.Equal(0, subs.ActivateCalls);
    }

    [Fact]
    public void HandleReturn_OrderNotFound_Fails()
    {
        var (svc, _, subs) = Build(PendingOrder("ORDER1"));
        var res = svc.HandleReturn(SignedCallback("KHONG_TON_TAI", "49000", "0"));
        Assert.False(res.Success);
        Assert.Equal(0, subs.ActivateCalls);
    }

    [Fact]
    public void HandleReturn_AlreadyPaid_Idempotent_NoDoubleActivate()
    {
        // Nguy cơ replay: gọi callback lần 2 cho đơn đã Paid không được kích hoạt gói thêm lần nữa
        var (svc, pays, subs) = Build(PendingOrder(status: "Paid"));
        var res = svc.HandleReturn(SignedCallback("ORDER1", "49000", "0"));
        Assert.True(res.Success);            // vẫn báo thành công (idempotent)
        Assert.Equal(0, subs.ActivateCalls); // nhưng KHÔNG kích hoạt lại
    }
}
