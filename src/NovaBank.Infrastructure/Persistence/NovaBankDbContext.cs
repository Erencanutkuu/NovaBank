using Microsoft.EntityFrameworkCore;

namespace NovaBank.Infrastructure.Persistence;

public sealed class NovaBankDbContext(DbContextOptions<NovaBankDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NovaBankDbContext).Assembly);
    }
}
