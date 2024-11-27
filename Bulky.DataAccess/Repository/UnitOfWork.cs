using System;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository;

public class UnitOfWork(DataContext context) : IUnitOfWork
{
    public ICategoryRepository CategoryRepository { get; private set; } = new CategoryRepository(context);
    public IProductRepository ProductRepository { get; private set; } = new ProductRepository(context);

    public void Save()
    {
        context.SaveChanges();;
    }
}