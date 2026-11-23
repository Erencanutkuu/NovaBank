using Microsoft.EntityFrameworkCore;
using NovaBank.Domain.Customers;

namespace NovaBank.Infrastructure.Persistence;

public sealed class NovaBankDbContext(DbContextOptions<NovaBankDbContext> options)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NovaBankDbContext).Assembly);
    }
}
