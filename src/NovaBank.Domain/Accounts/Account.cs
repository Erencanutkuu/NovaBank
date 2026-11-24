namespace NovaBank.Domain.Accounts;

public sealed class Account
{
    public const decimal MaxTransactionAmount = 1_000_000m;
    public const int AmountScale = 2; // Kuruş: en çok 2 ondalık.

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

        if (Balance != 0m)
        {
            throw new InvalidOperationException("Bakiyesi olan hesap kapatılamaz.");
        }

        Status = AccountStatus.Closed;
        ClosedAt = now;
        return true;
    }

    public static bool IsValidTransactionAmount(decimal amount) =>
        amount is > 0m and <= MaxTransactionAmount && decimal.Round(amount, AmountScale) == amount;

    public AccountTransaction Deposit(decimal amount, DateTimeOffset now)
    {
        EnsureCanMove(amount);

        Balance += amount;
        return AccountTransaction.Create(this, AccountTransactionType.Deposit, amount, now);
    }

    // Bakiye yetmezse hiçbir şeyi değiştirmeden null döner.
    public AccountTransaction? Withdraw(decimal amount, DateTimeOffset now)
    {
        EnsureCanMove(amount);

        if (amount > Balance)
        {
            return null;
        }

        Balance -= amount;
        return AccountTransaction.Create(this, AccountTransactionType.Withdrawal, amount, now);
    }

    // Servis ve validator bunları önceden kontrol eder; buraya takılmak programlama hatasıdır.
    private void EnsureCanMove(decimal amount)
    {
        if (Status == AccountStatus.Closed)
        {
            throw new InvalidOperationException("Kapalı hesapta para hareketi yapılamaz.");
        }

        if (!IsValidTransactionAmount(amount))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Geçersiz tutar.");
        }
    }
}
