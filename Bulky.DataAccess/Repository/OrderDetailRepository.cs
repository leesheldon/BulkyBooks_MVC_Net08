using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
{
    private DataContext _context;
    public OrderDetailRepository(DataContext context) : base(context)
    {
        _context = context;
    }

    public void Update(OrderDetail orderDetail)
    {
        _context.OrderDetails.Update(orderDetail);
    }
}
