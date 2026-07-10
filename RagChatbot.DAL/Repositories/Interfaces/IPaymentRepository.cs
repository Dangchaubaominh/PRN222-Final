using System.Collections.Generic;
using RagChatbot.DAL.Entities;

namespace RagChatbot.DAL.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        void Add(PaymentOrder order);
        PaymentOrder? GetByTxnRef(string vnpTxnRef);
        IEnumerable<PaymentOrder> GetByUser(int userId);
        void Update(PaymentOrder order);
    }
}
