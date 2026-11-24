using Microsoft.EntityFrameworkCore;
using NovaBank.Domain.Accounts;
using NovaBank.Domain.Customers;

namespace NovaBank.Infrastructure.Persistence;

public sealed class NovaBankDbContext(DbContextOptions<NovaBankDbContext> options)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NovaBankDbContext).Assembly);
    }
}
