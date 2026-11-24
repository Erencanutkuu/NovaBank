using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaBank.Api.IntegrationTests.Fixtures;
using Npgsql;

namespace NovaBank.Api.IntegrationTests;

public sealed class TransactionTests(DatabaseApiFixture api) : IClassFixture<DatabaseApiFixture>
{
    private const string AmountMessage = "Tutar 0'dan büyük, en çok 1.000.000 ve en çok 2 ondalıklı olmalıdır.";

    private async Task<Guid> OpenAccountAsync(string? balance = null)
    {
        var customer = await api.Client.PostJsonAsync("/api/customers",
            $$"""{"firstName":"Eren","lastName":"Utku","email":"{{Guid.NewGuid():N}}@example.com"}""");
        var customerId = (await customer.ReadDataAsync()).GetProperty("id").GetGuid();
        var account = await api.Client.PostJsonAsync($"/api/customers/{customerId}/accounts", """{"currency":"TRY"}""");
        var id = (await account.ReadDataAsync()).GetProperty("id").GetGuid();

        if (balance is not null)
        {
            Assert.Equal(HttpStatusCode.OK, (await DepositAsync(id, balance)).StatusCode);
        }

        return id;
    }

    private Task<HttpResponseMessage> DepositAsync(Guid id, string amount) =>
        api.Client.PostJsonAsync($"/api/accounts/{id}/deposit", $$"""{"amount":{{amount}}}""");

    private Task<HttpResponseMessage> WithdrawAsync(Guid id, string amount) =>
        api.Client.PostJsonAsync($"/api/accounts/{id}/withdraw", $$"""{"amount":{{amount}}}""");

    private Task<HttpResponseMessage> CloseAsync(Guid id) =>
        api.Client.PostAsync($"/api/accounts/{id}/close", content: null);

    private async Task<JsonElement> GetAccountAsync(Guid id) =>
        await (await api.Client.GetAsync($"/api/accounts/{id}")).ReadDataAsync();

    private async Task<decimal> GetBalanceAsync(Guid id) =>
        (await GetAccountAsync(id)).GetProperty("balance").GetDecimal();

    private async Task<JsonElement> ListAsync(Guid id, string query = "") =>
        await (await api.Client.GetAsync($"/api/accounts/{id}/transactions{query}")).ReadDataAsync();

    // Hareketleri eskiden yeniye yürüyüp her adımda bakiyeyi doğrular.
    private async Task AssertLedgerMatchesBalanceAsync(Guid id)
    {
        var running = 0m;
        foreach (var item in (await ListAsync(id, "?pageSize=100")).GetProperty("items").EnumerateArray().Reverse())
        {
            var amount = item.GetProperty("amount").GetDecimal();
            running += item.GetProperty("type").GetString() == "Deposit" ? amount : -amount;
            Assert.Equal(running, item.GetProperty("balanceAfter").GetDecimal());
            Assert.True(running >= 0);
        }

        Assert.Equal(running, await GetBalanceAsync(id));
    }

    [Fact]
    public async Task Deposit_ReturnsTransactionAndUpdatesBalance()
    {
        var id = await OpenAccountAsync("100");

        var response = await DepositAsync(id, "50.25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Para yatırıldı.", (await response.ReadJsonAsync()).GetProperty("message").GetString());
        var data = await response.ReadDataAsync();
        Assert.NotEqual(Guid.Empty, data.GetProperty("id").GetGuid());
        Assert.Equal(id, data.GetProperty("accountId").GetGuid());
        Assert.Equal("Deposit", data.GetProperty("type").GetString());
        Assert.Equal(50.25m, data.GetProperty("amount").GetDecimal());
        Assert.Equal(150.25m, data.GetProperty("balanceAfter").GetDecimal());
        Assert.Equal(150.25m, await GetBalanceAsync(id));
    }

    [Theory]
    [InlineData("30.10", "69.90")]
    [InlineData("100", "0")]
    public async Task Withdraw_ReturnsTransactionAndUpdatesBalance(string amount, string expected)
    {
        var id = await OpenAccountAsync("100");

        var response = await WithdrawAsync(id, amount);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Para çekildi.", (await response.ReadJsonAsync()).GetProperty("message").GetString());
        var data = await response.ReadDataAsync();
        Assert.Equal("Withdrawal", data.GetProperty("type").GetString());
        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture),
            data.GetProperty("amount").GetDecimal());
        var expectedBalance = decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expectedBalance, data.GetProperty("balanceAfter").GetDecimal());
        Assert.Equal(expectedBalance, await GetBalanceAsync(id));
    }

    [Fact]
    public async Task Withdraw_MoreThanBalance_Returns409AndRecordsNothing()
    {
        var id = await OpenAccountAsync("100");

        await (await WithdrawAsync(id, "100.01")).AssertProblemAsync(409, "7555");

        Assert.Equal(100m, await GetBalanceAsync(id));
        Assert.Equal(1, (await ListAsync(id)).GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.001")]
    [InlineData("10.555")]
    [InlineData("1000000.01")]
    [InlineData("9999999999999999999999999999")]
    public async Task Transactions_WithInvalidAmount_Return400(string amount)
    {
        var id = await OpenAccountAsync("100");

        foreach (var response in new[] { await DepositAsync(id, amount), await WithdrawAsync(id, amount) })
        {
            await response.AssertProblemAsync(400, "7000");
            Assert.Equal([AmountMessage], (await response.ReadJsonAsync()).GetProperty("errors")
                .GetProperty("amount").EnumerateArray().Select(x => x.GetString()!));
        }

        Assert.Equal(100m, await GetBalanceAsync(id));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"amount":null}""")]
    [InlineData("""{"amount":"abc"}""")]
    [InlineData("""{"amount":1e40}""")]
    [InlineData("")]
    public async Task Transactions_WithUnreadableBody_Return400(string json)
    {
        var id = await OpenAccountAsync();

        await (await api.Client.PostJsonAsync($"/api/accounts/{id}/deposit", json)).AssertProblemAsync(400, "7000");
        await (await api.Client.PostJsonAsync($"/api/accounts/{id}/withdraw", json)).AssertProblemAsync(400, "7000");
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("1000000")]
    [InlineData("10.500")] // Sondaki sıfırlar ondalık sayılmaz.
    public async Task Deposit_AcceptsBoundaryAmounts(string amount)
    {
        var id = await OpenAccountAsync();

        Assert.Equal(HttpStatusCode.OK, (await DepositAsync(id, amount)).StatusCode);

        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), await GetBalanceAsync(id));
    }

    [Fact]
    public async Task Transactions_OnUnknownAccount_Return404()
    {
        var id = Guid.NewGuid();

        await (await DepositAsync(id, "1")).AssertProblemAsync(404, "7557");
        await (await WithdrawAsync(id, "1")).AssertProblemAsync(404, "7557");
        await (await api.Client.GetAsync($"/api/accounts/{id}/transactions")).AssertProblemAsync(404, "7557");
    }

    [Fact]
    public async Task Transactions_OnClosedAccount_Return409()
    {
        var id = await OpenAccountAsync();
        await CloseAsync(id);

        await (await DepositAsync(id, "1")).AssertProblemAsync(409, "7556");
        await (await WithdrawAsync(id, "1")).AssertProblemAsync(409, "7556");
    }

    [Fact]
    public async Task List_ReturnsNewestFirstWithPaging()
    {
        var id = await OpenAccountAsync();
        await DepositAsync(await OpenAccountAsync(), "99"); // Başka hesabın hareketi listeye karışmamalı.
        await DepositAsync(id, "1");
        await DepositAsync(id, "2");
        var last = await (await WithdrawAsync(id, "3")).ReadDataAsync();

        var first = await ListAsync(id, "?page=1&pageSize=2");
        var second = await ListAsync(id, "?page=2&pageSize=2");
        var third = await ListAsync(id, "?page=3&pageSize=2");

        Assert.Equal([3m, 2m], Amounts(first));
        Assert.Equal([1m], Amounts(second));
        Assert.Empty(Amounts(third));
        Assert.Equal(3, first.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, second.GetProperty("page").GetInt32());
        Assert.Equal(2, second.GetProperty("pageSize").GetInt32());

        // POST cevabı ile listedeki kayıt aynıdır.
        Assert.Equal(last.GetProperty("id").GetGuid(), first.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(last.GetProperty("createdAt").GetRawText(),
            first.GetProperty("items")[0].GetProperty("createdAt").GetRawText());

        static decimal[] Amounts(JsonElement page) => page.GetProperty("items").EnumerateArray()
            .Select(x => x.GetProperty("amount").GetDecimal()).ToArray();
    }

    [Fact]
    public async Task List_DefaultsToFirstPageOf20AndAllowsEmptyAndClosedAccounts()
    {
        var id = await OpenAccountAsync();
        await CloseAsync(id);

        var data = await ListAsync(id);

        Assert.Equal(1, data.GetProperty("page").GetInt32());
        Assert.Equal(20, data.GetProperty("pageSize").GetInt32());
        Assert.Equal(0, data.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, data.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData("?page=0", "page", "Sayfa 1 ile 1.000.000 arasında olmalıdır.")]
    [InlineData("?page=1000001", "page", "Sayfa 1 ile 1.000.000 arasında olmalıdır.")]
    [InlineData("?pageSize=0", "pageSize", "Sayfa boyutu 1 ile 100 arasında olmalıdır.")]
    [InlineData("?pageSize=101", "pageSize", "Sayfa boyutu 1 ile 100 arasında olmalıdır.")]
    [InlineData("?page=abc", "page", "Geçersiz değer.")]
    [InlineData("?page=99999999999", "page", "Geçersiz değer.")]
    public async Task List_WithInvalidPaging_Returns400(string query, string field, string message)
    {
        var response = await api.Client.GetAsync($"/api/accounts/{await OpenAccountAsync()}/transactions{query}");

        await response.AssertProblemAsync(400, "7000");
        Assert.Equal([message], (await response.ReadJsonAsync()).GetProperty("errors")
            .GetProperty(field).EnumerateArray().Select(x => x.GetString()!));
    }

    [Fact]
    public async Task List_WithLargestPage_ReturnsEmpty()
    {
        var data = await ListAsync(await OpenAccountAsync("1"), "?page=1000000&pageSize=100");

        Assert.Equal(0, data.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Deposit_Concurrently_LosesNothing()
    {
        var id = await OpenAccountAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => DepositAsync(id, "10.05")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(201m, await GetBalanceAsync(id));
        await AssertLedgerMatchesBalanceAsync(id);
    }

    [Fact]
    public async Task Withdraw_Concurrently_NeverOverdraws()
    {
        var id = await OpenAccountAsync("75");

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => WithdrawAsync(id, "10")));

        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        foreach (var response in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
        {
            await response.AssertProblemAsync(409, "7555");
        }

        Assert.Equal(5m, await GetBalanceAsync(id));
        await AssertLedgerMatchesBalanceAsync(id);
    }

    [Fact]
    public async Task Close_WithBalance_Returns409AndStaysOpen()
    {
        var id = await OpenAccountAsync("0.01");

        await (await CloseAsync(id)).AssertProblemAsync(409, "7558");

        Assert.Equal("Open", (await GetAccountAsync(id)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Close_AfterWithdrawingEverything_Succeeds()
    {
        var id = await OpenAccountAsync("10");
        await WithdrawAsync(id, "10");

        Assert.Equal(HttpStatusCode.OK, (await CloseAsync(id)).StatusCode);
    }

    [Fact]
    public async Task CloseAndDeposit_Concurrently_NeverLeaveMoneyInClosedAccount()
    {
        for (var round = 0; round < 10; round++)
        {
            var id = await OpenAccountAsync();

            var close = CloseAsync(id);
            var deposits = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => DepositAsync(id, "1")));
            var closed = (await close).StatusCode == HttpStatusCode.OK;

            if (closed)
            {
                foreach (var response in deposits)
                {
                    await response.AssertProblemAsync(409, "7556");
                }
            }
            else
            {
                await (await close).AssertProblemAsync(409, "7558");
                Assert.All(deposits, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
            }

            Assert.Equal(closed ? 0m : 5m, await GetBalanceAsync(id));
            await AssertLedgerMatchesBalanceAsync(id);
        }
    }

    [Theory]
    [InlineData("UPDATE account_transactions SET amount = 1 WHERE account_id = {0}")]
    [InlineData("DELETE FROM account_transactions WHERE account_id = {0}")]
    [InlineData("TRUNCATE account_transactions")]
    public async Task Database_RejectsChangingTransactions(string sql)
    {
        var id = await OpenAccountAsync("100");

        await api.ExecuteAsync(async context =>
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => context.Database.ExecuteSqlRawAsync(sql, id));
            Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
        });

        Assert.Equal(1, (await ListAsync(id)).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Database_RejectsNegativeBalance()
    {
        var id = await OpenAccountAsync("100");

        await api.ExecuteAsync(async context =>
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                context.Database.ExecuteSqlRawAsync("UPDATE accounts SET balance = -1 WHERE id = {0}", id));
            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        });
    }
}
