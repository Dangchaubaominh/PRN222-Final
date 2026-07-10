using System.Collections.Generic;
using System.Linq;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class PackageRepository : IPackageRepository
    {
        private readonly ApplicationDbContext _context;

        public PackageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Package> GetAll()
            => _context.Packages.OrderBy(p => p.Price).ToList();

        public IEnumerable<Package> GetActive()
            => _context.Packages.Where(p => p.IsActive).OrderBy(p => p.Price).ToList();

        public Package? GetById(int id)
            => _context.Packages.Find(id);

        public void Add(Package package)
        {
            _context.Packages.Add(package);
            _context.SaveChanges();
        }

        public void Update(Package package)
        {
            _context.Packages.Update(package);
            _context.SaveChanges();
        }
    }
}
