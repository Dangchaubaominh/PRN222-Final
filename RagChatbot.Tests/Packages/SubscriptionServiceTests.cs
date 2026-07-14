using System;
using System.Linq;
using RagChatbot.BLL.Services.Implements;
using RagChatbot.DAL.Entities;
using Xunit;

namespace RagChatbot.Tests.Packages;

public class SubscriptionServiceTests
{
    private static (SubscriptionService svc, FakeSubscriptionRepository subs, FakePackageRepository pkgs) Build()
    {
        var subs = new FakeSubscriptionRepository();
        var pkgs = new FakePackageRepository();
        pkgs.Items.Add(new Package { Id = 1, Name = "Free", Price = 0, TokenQuota = 50_000, AllowedModels = "m", DurationDays = 30, IsActive = true });
        pkgs.Items.Add(new Package { Id = 2, Name = "Basic", Price = 49000, TokenQuota = 500_000, AllowedModels = "m", DurationDays = 30, IsActive = true });
        pkgs.Items.Add(new Package { Id = 3, Name = "Pro", Price = 99000, TokenQuota = 2_000_000, AllowedModels = "m", DurationDays = 30, IsActive = true });
        return (new SubscriptionService(subs, pkgs), subs, pkgs);
    }

    [Fact]
    public void GetRemainingQuota_NoSubscription_ReturnsZero()
    {
        var (svc, _, _) = Build();
        Assert.Equal(0, svc.GetRemainingQuota(userId: 10));
    }

    [Fact]
    public void GetRemainingQuota_ReturnsQuotaMinusUsed()
    {
        var (svc, subs, _) = Build();
        subs.Items.Add(new UserSubscription { UserId = 10, PackageId = 2, TokensUsed = 120_000, Status = "Active", ExpireAt = DateTime.UtcNow.AddDays(10) });
        Assert.Equal(380_000, svc.GetRemainingQuota(10)); // 500k - 120k
    }

    [Fact]
    public void GetRemainingQuota_UsedExceedsQuota_ReturnsZeroNotNegative()
    {
        var (svc, subs, _) = Build();
        subs.Items.Add(new UserSubscription { UserId = 10, PackageId = 1, TokensUsed = 60_000, Status = "Active", ExpireAt = DateTime.UtcNow.AddDays(10) });
        Assert.Equal(0, svc.GetRemainingQuota(10)); // 50k - 60k -> 0, không âm
    }

    [Fact]
    public void GetRemainingQuota_ExpiredSubscription_ReturnsZero()
    {
        var (svc, subs, _) = Build();
        subs.Items.Add(new UserSubscription { UserId = 10, PackageId = 2, TokensUsed = 0, Status = "Active", ExpireAt = DateTime.UtcNow.AddDays(-1) });
        Assert.Equal(0, svc.GetRemainingQuota(10)); // đã hết hạn -> không tính
    }

    [Fact]
    public void AddUsedTokens_NoSubscription_DoesNotThrow()
    {
        var (svc, _, _) = Build();
        svc.AddUsedTokens(10, 1000); // không có gói -> no-op, không lỗi
    }

    [Fact]
    public void AddUsedTokens_Accumulates()
    {
        var (svc, subs, _) = Build();
        subs.Items.Add(new UserSubscription { UserId = 10, PackageId = 2, TokensUsed = 0, Status = "Active", ExpireAt = DateTime.UtcNow.AddDays(10) });
        svc.AddUsedTokens(10, 300);
        svc.AddUsedTokens(10, 200);
        Assert.Equal(500, subs.Items.Single().TokensUsed);
    }

    [Fact]
    public void AddUsedTokens_NonPositive_Ignored()
    {
        var (svc, subs, _) = Build();
        subs.Items.Add(new UserSubscription { UserId = 10, PackageId = 2, TokensUsed = 100, Status = "Active", ExpireAt = DateTime.UtcNow.AddDays(10) });
        svc.AddUsedTokens(10, 0);
        svc.AddUsedTokens(10, -50);
        Assert.Equal(100, subs.Items.Single().TokensUsed);
    }

    [Fact]
    public void ActivateOrRenew_NoPrior_CreatesActiveWithResetQuotaAndExpiry()
    {
        var (svc, subs, _) = Build();
        svc.ActivateOrRenew(userId: 10, packageId: 2);

        var s = Assert.Single(subs.Items);
        Assert.Equal("Active", s.Status);
        Assert.Equal(0, s.TokensUsed);
        Assert.True(s.ExpireAt > DateTime.UtcNow.AddDays(29) && s.ExpireAt < DateTime.UtcNow.AddDays(31));
    }

    [Fact]
    public void ActivateOrRenew_WithPrior_LeavesExactlyOneActive()
    {
        var (svc, subs, _) = Build();
        svc.ActivateOrRenew(10, 2); // Basic
        svc.ActivateOrRenew(10, 3); // đổi sang Pro

        Assert.Equal(1, subs.Items.Count(s => s.Status == "Active"));
        Assert.Equal(3, subs.Items.Single(s => s.Status == "Active").PackageId);
    }

    [Fact]
    public void ActivateOrRenew_ResetsUsedTokens()
    {
        var (svc, subs, _) = Build();
        subs.Items.Add(new UserSubscription { UserId = 10, PackageId = 2, TokensUsed = 400_000, Status = "Active", ExpireAt = DateTime.UtcNow.AddDays(5) });
        svc.ActivateOrRenew(10, 2); // mua/gia hạn lại -> quota kỳ mới về 0
        Assert.Equal(0, subs.Items.Single(s => s.Status == "Active").TokensUsed);
        Assert.Equal(500_000, svc.GetRemainingQuota(10)); // full quota lại
    }

    [Fact]
    public void ActivateOrRenew_InvalidPackage_NoChange()
    {
        var (svc, subs, _) = Build();
        svc.ActivateOrRenew(10, packageId: 999); // gói không tồn tại
        Assert.Empty(subs.Items);
    }
}
