using Microsoft.EntityFrameworkCore;

namespace SecPerf.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Add DbSet<TEntity> properties here, for example:
    // public DbSet<User> Users { get; set; } = null!;
}
