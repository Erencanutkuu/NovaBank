using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Transactions;

public interface ITransactionRepository
{
    // IUnitOfWorkTransaction.CommitAsync çağrılınca yazılır.
    void Add(AccountTransaction transaction);

    // En yeni hareket önce gelir.
    Task<IReadOnlyList<AccountTransaction>> ListAsync(
        Guid accountId, int skip, int take, CancellationToken cancellationToken);

    Task<int> CountAsync(Guid accountId, CancellationToken cancellationToken);
}
