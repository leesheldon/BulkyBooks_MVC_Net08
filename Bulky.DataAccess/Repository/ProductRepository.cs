using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class ProductRepository : Repository<Product>, IProductRepository
{
    private DataContext _context;
    public ProductRepository(DataContext context) : base(context)
    {
        _context = context;
    }

    public void Old_Update(Product product)
    {
        _context.Products.Update(product);
    }

    public void Update(Product product)
    {
        var productFromDb = _context.Products.FirstOrDefault(x => x.Id == product.Id);
        if (productFromDb != null)
        {
            productFromDb.Title = product.Title;
            productFromDb.ISBN = product.ISBN;
            productFromDb.Price = product.Price;
            productFromDb.Price50 = product.Price50;
            productFromDb.Price100 = product.Price100;
            productFromDb.ListPrice = product.ListPrice;
            productFromDb.Description = product.Description;
            productFromDb.CategoryId = product.CategoryId;
            productFromDb.Author = product.Author;
            productFromDb.ProductImages = product.ProductImages;
        }
    }
}
