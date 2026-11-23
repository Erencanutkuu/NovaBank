using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Accounts;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken cancellationToken);

    Task<Account?> FindAsync(Guid id, CancellationToken cancellationToken);

    // Açılış sırasıyla döner.
    Task<IReadOnlyList<Account>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    // Hesap okunduktan sonra başka bir istek onu değiştirdiyse kaydetmez ve false döner.
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}
