using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
{
    private DataContext _context;
    public ApplicationUserRepository(DataContext context) : base(context)
    {
        _context = context;
    }

    public void Update(ApplicationUser user)
    {
        _context.ApplicationUsers.Update(user);
    }
}
