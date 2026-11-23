using NovaBank.Application.Common;
using NovaBank.Application.Customers;
using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Accounts;

public sealed class AccountService(
    IAccountRepository accounts,
    ICustomerRepository customers,
    TimeProvider timeProvider)
{
    public async Task<AccountDto> OpenAsync(
        Guid customerId, OpenAccountRequest request, CancellationToken cancellationToken)
    {
        await EnsureCustomerExistsAsync(customerId, cancellationToken);

        var account = Account.Open(
            customerId, Enum.Parse<Currency>(request.Currency), timeProvider.GetUtcNowInMicroseconds());

        await accounts.AddAsync(account, cancellationToken);

        return AccountDto.From(account);
    }

    public async Task<AccountDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        AccountDto.From(await FindAsync(id, cancellationToken));

    public async Task<IReadOnlyList<AccountDto>> ListAsync(Guid customerId, CancellationToken cancellationToken)
    {
        await EnsureCustomerExistsAsync(customerId, cancellationToken);

        var items = await accounts.ListByCustomerAsync(customerId, cancellationToken);

        return items.Select(AccountDto.From).ToList();
    }

    public async Task<AccountDto> CloseAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await FindAsync(id, cancellationToken);

        // Bugün hesabı değiştiren tek işlem kapatma. Çakışma varsa hesabı başka bir istek kapatmıştır.
        if (!account.Close(timeProvider.GetUtcNowInMicroseconds()) ||
            !await accounts.TrySaveChangesAsync(cancellationToken))
        {
            throw AccountErrors.Closed();
        }

        return AccountDto.From(account);
    }

    private async Task<Account> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await accounts.FindAsync(id, cancellationToken) ?? throw AccountErrors.NotFound();

    private async Task EnsureCustomerExistsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        if (!await customers.ExistsAsync(customerId, cancellationToken))
        {
            throw CustomerErrors.NotFound();
        }
    }
}
