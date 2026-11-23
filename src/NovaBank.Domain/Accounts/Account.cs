namespace NovaBank.Domain.Accounts;

public sealed class Account
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Iban Iban { get; private set; } = null!;
    public Currency Currency { get; private set; }
    public decimal Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private Account() { } // EF Core ve Open metodu kullanır.

    public static Account Open(Guid customerId, Currency currency, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        Iban = Iban.Generate(),
        Currency = currency,
        Balance = 0m,
        Status = AccountStatus.Open,
        OpenedAt = now
    };

    // Hesap zaten kapalıysa hiçbir şeyi değiştirmeden false döner.
    public bool Close(DateTimeOffset now)
    {
        if (Status == AccountStatus.Closed)
        {
            return false;
        }

        Status = AccountStatus.Closed;
        ClosedAt = now;
        return true;
    }
}
