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
}
