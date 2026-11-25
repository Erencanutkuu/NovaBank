using Microsoft.EntityFrameworkCore;
using Npgsql;
using NovaBank.Application.Customers;
using NovaBank.Domain.Customers;
using NovaBank.Infrastructure.Persistence.Configurations;

namespace NovaBank.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(NovaBankDbContext context) : ICustomerRepository
{
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        context.Customers.Add(customer);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: CustomerConfiguration.EmailIndexName
        })
        {
            throw CustomerErrors.EmailAlreadyExists();
        }
    }

    public Task<Customer?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> ListAsync(int skip, int take, CancellationToken cancellationToken) =>
        await context.Customers
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        context.Customers.CountAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Customers.AnyAsync(x => x.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        context.Customers.AnyAsync(x => x.Email == email, cancellationToken);
}
