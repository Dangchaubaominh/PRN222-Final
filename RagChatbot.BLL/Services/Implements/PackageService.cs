using System.Collections.Generic;
using System.Linq;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class PackageService : IPackageService
    {
        private readonly IPackageRepository _repo;

        public PackageService(IPackageRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<PackageDto> GetActive() => _repo.GetActive().Select(ToDto);

        public IEnumerable<PackageDto> GetAll() => _repo.GetAll().Select(ToDto);

        public PackageDto? GetById(int id)
        {
            var p = _repo.GetById(id);
            return p == null ? null : ToDto(p);
        }

        public void Create(PackageDto dto)
        {
            _repo.Add(new Package
            {
                Name = dto.Name,
                Price = dto.Price,
                TokenQuota = dto.TokenQuota,
                AllowedModels = dto.AllowedModels,
                DurationDays = dto.DurationDays,
                IsActive = dto.IsActive
            });
        }

        public void Update(PackageDto dto)
        {
            var p = _repo.GetById(dto.Id);
            if (p == null) return;
            p.Name = dto.Name;
            p.Price = dto.Price;
            p.TokenQuota = dto.TokenQuota;
            p.AllowedModels = dto.AllowedModels;
            p.DurationDays = dto.DurationDays;
            p.IsActive = dto.IsActive;
            _repo.Update(p);
        }

        public void ToggleActive(int id)
        {
            var p = _repo.GetById(id);
            if (p == null) return;
            p.IsActive = !p.IsActive;
            _repo.Update(p);
        }

        private static PackageDto ToDto(Package p) => new PackageDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            TokenQuota = p.TokenQuota,
            AllowedModels = p.AllowedModels,
            DurationDays = p.DurationDays,
            IsActive = p.IsActive
        };
    }
}
