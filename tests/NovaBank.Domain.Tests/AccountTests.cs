using NovaBank.Domain.Accounts;

namespace NovaBank.Domain.Tests;

public sealed class AccountTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Open_StartsOpenWithZeroBalance()
    {
        var customerId = Guid.NewGuid();

        var account = Account.Open(customerId, Currency.USD, OpenedAt);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(customerId, account.CustomerId);
        Assert.Equal(Currency.USD, account.Currency);
        Assert.Equal(0m, account.Balance);
        Assert.Equal(AccountStatus.Open, account.Status);
        Assert.Equal(OpenedAt, account.OpenedAt);
        Assert.Null(account.ClosedAt);
        Assert.True(Iban.IsValid(account.Iban.Value));
    }

    [Fact]
    public void Close_ClosesOpenAccount()
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);

        Assert.True(account.Close(OpenedAt.AddDays(1)));

        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.Equal(OpenedAt.AddDays(1), account.ClosedAt);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ReturnsFalseAndKeepsFirstDate()
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);
        account.Close(OpenedAt.AddDays(1));

        Assert.False(account.Close(OpenedAt.AddDays(2)));

        Assert.Equal(OpenedAt.AddDays(1), account.ClosedAt);
    }

    [Fact]
    public void Deposit_IncreasesBalanceAndRecordsTransaction()
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);
        account.Deposit(100m, OpenedAt);

        var transaction = account.Deposit(50.25m, OpenedAt.AddDays(1));

        Assert.Equal(150.25m, account.Balance);
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(account.Id, transaction.AccountId);
        Assert.Equal(AccountTransactionType.Deposit, transaction.Type);
        Assert.Equal(50.25m, transaction.Amount);
        Assert.Equal(150.25m, transaction.BalanceAfter);
        Assert.Equal(OpenedAt.AddDays(1), transaction.CreatedAt);
    }

    [Theory]
    [InlineData(30.10, 69.90)]
    [InlineData(100, 0)] // Bakiyenin tamamı çekilebilir.
    public void Withdraw_DecreasesBalanceAndRecordsTransaction(decimal amount, decimal expectedBalance)
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);
        account.Deposit(100m, OpenedAt);

        var transaction = account.Withdraw(amount, OpenedAt.AddDays(1));

        Assert.NotNull(transaction);
        Assert.Equal(expectedBalance, account.Balance);
        Assert.Equal(account.Id, transaction.AccountId);
        Assert.Equal(AccountTransactionType.Withdrawal, transaction.Type);
        Assert.Equal(amount, transaction.Amount); // Çekmede de tutar artıdır; yönü Type belirtir.
        Assert.Equal(expectedBalance, transaction.BalanceAfter);
        Assert.Equal(OpenedAt.AddDays(1), transaction.CreatedAt);
    }

    [Fact]
    public void Withdraw_MoreThanBalance_ReturnsNullAndKeepsBalance()
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);
        account.Deposit(100m, OpenedAt);

        Assert.Null(account.Withdraw(100.01m, OpenedAt));

        Assert.Equal(100m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.001)]
    [InlineData(1000000.01)]
    public void Transactions_WithInvalidAmount_Throw(decimal amount)
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);
        account.Deposit(100m, OpenedAt);

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(amount, OpenedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => account.Withdraw(amount, OpenedAt));

        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void Deposit_AcceptsMaximumAmount()
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);

        account.Deposit(1_000_000m, OpenedAt);

        Assert.Equal(1_000_000m, account.Balance);
    }

    [Fact]
    public void Transactions_OnClosedAccount_Throw()
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);
        account.Close(OpenedAt);

        Assert.Throws<InvalidOperationException>(() => account.Deposit(1m, OpenedAt));
        Assert.Throws<InvalidOperationException>(() => account.Withdraw(1m, OpenedAt));
    }

    [Fact]
    public void Close_WithBalance_ThrowsAndStaysOpen()
    {
        var account = Account.Open(Guid.NewGuid(), Currency.TRY, OpenedAt);
        account.Deposit(0.01m, OpenedAt);

        Assert.Throws<InvalidOperationException>(() => account.Close(OpenedAt));

        Assert.Equal(AccountStatus.Open, account.Status);
        Assert.Null(account.ClosedAt);
    }
}
