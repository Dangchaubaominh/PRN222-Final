using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.Tests.Packages;

// ---- Fake repositories (in-memory, không cần DB) ----

internal class FakePackageRepository : IPackageRepository
{
    public readonly List<Package> Items = new();
    public IEnumerable<Package> GetAll() => Items;
    public IEnumerable<Package> GetActive() => Items.Where(p => p.IsActive);
    public Package? GetById(int id) => Items.FirstOrDefault(p => p.Id == id);
    public void Add(Package p) { if (p.Id == 0) p.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1; Items.Add(p); }
    public void Update(Package p) { /* tham chiếu in-memory, không cần làm gì */ }
}

internal class FakeSubscriptionRepository : ISubscriptionRepository
{
    public readonly List<UserSubscription> Items = new();
    public UserSubscription? GetActiveByUser(int userId) =>
        Items.Where(s => s.UserId == userId && s.Status == "Active" && s.ExpireAt > DateTime.UtcNow)
             .OrderByDescending(s => s.ExpireAt)
             .FirstOrDefault();
    public void Add(UserSubscription s) { if (s.Id == 0) s.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1; Items.Add(s); }
    public void Update(UserSubscription s) { }
}

internal class FakePaymentRepository : IPaymentRepository
{
    public readonly List<PaymentOrder> Items = new();
    public void Add(PaymentOrder o) { if (o.Id == 0) o.Id = Items.Count == 0 ? 1 : Items.Max(x => x.Id) + 1; Items.Add(o); }
    public PaymentOrder? GetByTxnRef(string r) => Items.FirstOrDefault(o => o.VnpTxnRef == r);
    public IEnumerable<PaymentOrder> GetByUser(int userId) => Items.Where(o => o.UserId == userId);
    public void Update(PaymentOrder o) { }
}

// Spy: đếm số lần kích hoạt gói (để test callback không kích hoạt 2 lần)
internal class SpySubscriptionService : ISubscriptionService
{
    public int ActivateCalls;
    public (int userId, int packageId)? LastActivate;
    public UserSubscription? GetActive(int userId) => null;
    public long GetRemainingQuota(int userId) => 0;
    public void AddUsedTokens(int userId, long tokens) { }
    public void ActivateOrRenew(int userId, int packageId) { ActivateCalls++; LastActivate = (userId, packageId); }
}

// IConfiguration tối giản chỉ hỗ trợ indexer (đủ cho MoMoService)
internal class FakeConfig : IConfiguration
{
    private readonly Dictionary<string, string?> _d;
    public FakeConfig(Dictionary<string, string?> d) => _d = d;
    public string? this[string key] { get => _d.TryGetValue(key, out var v) ? v : null; set => _d[key] = value; }
    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
    public IChangeToken GetReloadToken() => throw new NotSupportedException();
    public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
}
