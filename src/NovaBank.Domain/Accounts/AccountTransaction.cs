namespace NovaBank.Domain.Accounts;

// Hesap hareketi. Yalnızca Account oluşturur ve sonradan değişmez.
public sealed class AccountTransaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public AccountTransactionType Type { get; private set; }
    public decimal Amount { get; private set; } // Her zaman artıdır; yönü Type belirtir.
    public decimal BalanceAfter { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AccountTransaction() { } // EF Core ve Create metodu kullanır.

    internal static AccountTransaction Create(
        Account account, AccountTransactionType type, decimal amount, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = account.Id,
        Type = type,
        Amount = amount,
        BalanceAfter = account.Balance,
        CreatedAt = now
    };
}
