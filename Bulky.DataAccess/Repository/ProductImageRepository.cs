using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
{
    private DataContext _context;
    public ProductImageRepository(DataContext context) : base(context)
    {
        _context = context;
    }

    public void Update(ProductImage productImage)
    {
        _context.ProductImages.Update(productImage);
    }
}
