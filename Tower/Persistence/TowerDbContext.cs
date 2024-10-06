using Microsoft.EntityFrameworkCore;

namespace Tower.Persistence;
public class TowerDbContext : DbContext
{
    public TowerDbContext(DbContextOptions<TowerDbContext> options)
        : base(options)
    {
    }
}
