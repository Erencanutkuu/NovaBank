using Microsoft.EntityFrameworkCore;
using NovaBank.Application.Accounts;
using NovaBank.Domain.Accounts;

namespace NovaBank.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(NovaBankDbContext context) : IAccountRepository
{
    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<Account?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListByCustomerAsync(
        Guid customerId, CancellationToken cancellationToken) =>
        await context.Accounts
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.OpenedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
