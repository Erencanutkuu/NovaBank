using NovaBank.Domain.Accounts;

namespace NovaBank.Application.Transactions;

public sealed record TransactionDto(
    Guid Id,
    Guid AccountId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    DateTimeOffset CreatedAt)
{
    public static TransactionDto From(AccountTransaction transaction) => new(
        transaction.Id,
        transaction.AccountId,
        transaction.Type.ToString(),
        transaction.Amount,
        transaction.BalanceAfter,
        transaction.CreatedAt);
}
