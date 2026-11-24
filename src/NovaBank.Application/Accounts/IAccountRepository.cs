using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Accounts;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken cancellationToken);

    Task<Account?> FindAsync(Guid id, CancellationToken cancellationToken);

    // Açılış sırasıyla döner.
    Task<IReadOnlyList<Account>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    // Hesabı, açık IUnitOfWork işlemi bitene kadar kilitleyerek okur. Hesabı değiştiren her işlem bunu kullanır.
    Task<Account?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);
}
