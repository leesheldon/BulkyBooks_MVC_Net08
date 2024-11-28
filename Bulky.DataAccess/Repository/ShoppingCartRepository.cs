using System;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
{
    private DataContext _context;
    public ShoppingCartRepository(DataContext context) : base(context)
    {
        _context = context;
    }

    public void Update(ShoppingCart shoppingCart)
    {
        _context.ShoppingCarts.Update(shoppingCart);
    }
}
