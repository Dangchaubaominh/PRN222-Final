using System.Collections.Generic;
using RagChatbot.DAL.Entities;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface IPackageRepository
    {
        IEnumerable<Package> GetAll();
        IEnumerable<Package> GetActive();
        Package? GetById(int id);
        void Add(Package package);
        void Update(Package package);
    }
}
