using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
{
    private DataContext _context;
    public OrderHeaderRepository(DataContext context) : base(context)
    {
        _context = context;
    }

    public void Update(OrderHeader orderHeader)
    {
        _context.OrderHeaders.Update(orderHeader);
    }

    public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
    {
        var orderFromDb = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);
        if (orderFromDb != null) {
            orderFromDb.OrderStatus = orderStatus;

            if (!string.IsNullOrEmpty(paymentStatus)) {
                orderFromDb.PaymentStatus = paymentStatus;
            }
        }
    }

    public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
    {
        var orderFromDb = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);
        
        if (orderFromDb != null) {
            if (!string.IsNullOrEmpty(sessionId)) {
                orderFromDb.SessionId = sessionId;
            }

            if (!string.IsNullOrEmpty(paymentIntentId)) {
                orderFromDb.PaymentIntentId = paymentIntentId;
                orderFromDb.PaymentDate = DateTime.Now;
            }
        }
    }
}
