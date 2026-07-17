using System.Collections.Generic;
using System.Linq;
using RagChatbot.DAL.Data;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.DAL.Repositories.Implements
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(PaymentOrder order)
        {
            _context.PaymentOrders.Add(order);
            _context.SaveChanges();
        }

        public PaymentOrder? GetByTxnRef(string vnpTxnRef)
            => _context.PaymentOrders.FirstOrDefault(o => o.VnpTxnRef == vnpTxnRef);

        public IEnumerable<PaymentOrder> GetByUser(int userId)
            => _context.PaymentOrders.Where(o => o.UserId == userId)
                                     .OrderByDescending(o => o.CreatedAt)
                                     .ToList();

        public void Update(PaymentOrder order)
        {
            _context.PaymentOrders.Update(order);
            _context.SaveChanges();
        }
    }
}
