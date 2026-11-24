using Microsoft.EntityFrameworkCore;
using NovaBank.Application.Transactions;
using NovaBank.Domain.Accounts;
using NovaBank.Infrastructure.Persistence.Configurations;

namespace NovaBank.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository(NovaBankDbContext context) : ITransactionRepository
{
    public void Add(AccountTransaction transaction) => context.AccountTransactions.Add(transaction);

    public async Task<IReadOnlyList<AccountTransaction>> ListAsync(
        Guid accountId, int skip, int take, CancellationToken cancellationToken) =>
        await context.AccountTransactions
            .AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => EF.Property<long>(x, AccountTransactionConfiguration.SequenceProperty))
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.AccountTransactions.CountAsync(x => x.AccountId == accountId, cancellationToken);
}
