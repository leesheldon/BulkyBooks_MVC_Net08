using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository;

public class UnitOfWork(DataContext context) : IUnitOfWork
{
    public ICategoryRepository CategoryRepository { get; private set; } = new CategoryRepository(context);
    public IProductRepository ProductRepository { get; private set; } = new ProductRepository(context);
    public ICompanyRepository CompanyRepository { get; private set; } = new CompanyRepository(context);
    public IShoppingCartRepository ShoppingCartRepository { get; private set; } = new ShoppingCartRepository(context);
    public IApplicationUserRepository ApplicationUserRepository { get; private set; } = new ApplicationUserRepository(context);
    public IOrderHeaderRepository OrderHeaderRepository { get; private set; } = new OrderHeaderRepository(context);
    public IOrderDetailRepository OrderDetailRepository { get; private set; } = new OrderDetailRepository(context);
    public IProductImageRepository ProductImageRepository { get; private set; } = new ProductImageRepository(context);

    public void Save()
    {
        context.SaveChanges();;
    }
}
