using System.Collections.Generic;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IPackageService
    {
        IEnumerable<PackageDto> GetActive();   // gói đang bán (cho Học sinh)
        IEnumerable<PackageDto> GetAll();       // cho Admin
        PackageDto? GetById(int id);
        void Create(PackageDto dto);
        void Update(PackageDto dto);
        void ToggleActive(int id);
    }
}
