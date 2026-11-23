using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Accounts;

public sealed record AccountDto(
    Guid Id,
    Guid CustomerId,
    string Iban,
    string Currency,
    decimal Balance,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt)
{
    public static AccountDto From(Account account) => new(
        account.Id,
        account.CustomerId,
        account.Iban.Value,
        account.Currency.ToString(),
        account.Balance,
        account.Status.ToString(),
        account.OpenedAt,
        account.ClosedAt);
}
