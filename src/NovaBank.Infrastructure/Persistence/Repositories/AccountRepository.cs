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

    public async Task<Account?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        // Kilit yalnızca bir işlem açıkken anlamlıdır; yoksa sorgu biter bitmez bırakılır.
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("FindForUpdateAsync için önce IUnitOfWork.BeginAsync çağrılmalıdır.");
        }

        // EF Core'da karşılığı olmadığı için SQL ile yazıldı. xmin gizli sütundur, "*" içine girmez.
        var rows = await context.Accounts
            .FromSql($"SELECT *, xmin FROM accounts WHERE id = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }
}
