using System;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subRepo;
        private readonly IPackageRepository _packageRepo;

        public SubscriptionService(ISubscriptionRepository subRepo, IPackageRepository packageRepo)
        {
            _subRepo = subRepo;
            _packageRepo = packageRepo;
        }

        public UserSubscription? GetActive(int userId) => _subRepo.GetActiveByUser(userId);

        public long GetRemainingQuota(int userId)
        {
            var active = _subRepo.GetActiveByUser(userId);
            if (active == null) return 0;

            var package = _packageRepo.GetById(active.PackageId);
            if (package == null) return 0;

            long remaining = package.TokenQuota - active.TokensUsed;
            return remaining > 0 ? remaining : 0;
        }

        public void AddUsedTokens(int userId, long tokens)
        {
            if (tokens <= 0) return;
            var active = _subRepo.GetActiveByUser(userId);
            if (active == null) return;

            active.TokensUsed += tokens;
            _subRepo.Update(active);
        }

        public void ActivateOrRenew(int userId, int packageId)
        {
            var package = _packageRepo.GetById(packageId);
            if (package == null) return;

            // Hết hiệu lực gói cũ (mỗi thời điểm chỉ 1 gói Active)
            var current = _subRepo.GetActiveByUser(userId);
            if (current != null)
            {
                current.Status = "Expired";
                _subRepo.Update(current);
            }

            _subRepo.Add(new UserSubscription
            {
                UserId = userId,
                PackageId = packageId,
                StartAt = DateTime.UtcNow,
                ExpireAt = DateTime.UtcNow.AddDays(package.DurationDays),
                TokensUsed = 0,
                Status = "Active"
            });
        }
    }
}
