using System.Net;
using System.Text.Json;
using NovaBank.Api.IntegrationTests.Fixtures;
using NovaBank.Domain.Accounts;

namespace NovaBank.Api.IntegrationTests;

public sealed class AccountTests(DatabaseApiFixture api) : IClassFixture<DatabaseApiFixture>
{
    private async Task<Guid> CreateCustomerAsync()
    {
        var response = await api.Client.PostJsonAsync("/api/customers",
            $$"""{"firstName":"Eren","lastName":"Utku","email":"{{Guid.NewGuid():N}}@example.com"}""");
        return (await response.ReadDataAsync()).GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> OpenAsync(Guid customerId, string currency = "TRY") =>
        api.Client.PostJsonAsync($"/api/customers/{customerId}/accounts", $$"""{"currency":"{{currency}}"}""");

    private async Task<Guid> OpenAndGetIdAsync() =>
        (await (await OpenAsync(await CreateCustomerAsync())).ReadDataAsync()).GetProperty("id").GetGuid();

    private Task<HttpResponseMessage> CloseAsync(Guid id) =>
        api.Client.PostAsync($"/api/accounts/{id}/close", content: null);

    [Theory]
    [InlineData("TRY")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public async Task Open_Returns201WithEmptyOpenAccount(string currency)
    {
        var customerId = await CreateCustomerAsync();

        var response = await OpenAsync(customerId, currency);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.ReadDataAsync();
        Assert.Equal(customerId, data.GetProperty("customerId").GetGuid());
        Assert.True(Iban.IsValid(data.GetProperty("iban").GetString()));
        Assert.Equal(currency, data.GetProperty("currency").GetString());
        Assert.Equal(0m, data.GetProperty("balance").GetDecimal());
        Assert.Equal("Open", data.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("closedAt").ValueKind);
    }

    [Fact]
    public async Task Open_LocationReturnsSameAccount()
    {
        var opened = await OpenAsync(await CreateCustomerAsync());

        var response = await api.Client.GetAsync(opened.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expected = await opened.ReadDataAsync();
        var actual = await response.ReadDataAsync();
        foreach (var property in expected.EnumerateObject().Where(p => p.Name != "balance"))
        {
            Assert.Equal(property.Value.GetRawText(), actual.GetProperty(property.Name).GetRawText());
        }

        Assert.Equal(expected.GetProperty("balance").GetDecimal(), actual.GetProperty("balance").GetDecimal());
    }

    [Theory]
    [InlineData("""{"currency":"GBP"}""")]
    [InlineData("""{"currency":"try"}""")]
    [InlineData("""{"currency":"0"}""")]
    [InlineData("""{"currency":"TRY,USD"}""")]
    [InlineData("""{"currency":" TRY"}""")]
    [InlineData("""{"currency":""}""")]
    [InlineData("""{"currency":null}""")]
    [InlineData("{}")]
    public async Task Open_WithInvalidCurrency_Returns400(string json)
    {
        var response = await api.Client.PostJsonAsync($"/api/customers/{await CreateCustomerAsync()}/accounts", json);

        await response.AssertProblemAsync(400, "7000");
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.Equal(["Para birimi TRY, USD veya EUR olmalıdır."],
            errors.GetProperty("currency").EnumerateArray().Select(x => x.GetString()!));
    }

    [Fact]
    public async Task Open_ForUnknownCustomer_Returns404()
    {
        await (await OpenAsync(Guid.NewGuid())).AssertProblemAsync(404, "7232");
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnAccountsInOpeningOrder()
    {
        var customerId = await CreateCustomerAsync();
        await OpenAsync(await CreateCustomerAsync());
        await OpenAsync(customerId, "USD");
        await OpenAsync(customerId, "TRY");
        await OpenAsync(customerId, "TRY"); // Aynı para biriminde ikinci hesap açılabilir.

        var data = await (await api.Client.GetAsync($"/api/customers/{customerId}/accounts")).ReadDataAsync();

        Assert.Equal(["USD", "TRY", "TRY"],
            data.EnumerateArray().Select(x => x.GetProperty("currency").GetString()!));
    }

    [Fact]
    public async Task List_ForCustomerWithoutAccounts_ReturnsEmptyArray()
    {
        var response = await api.Client.GetAsync($"/api/customers/{await CreateCustomerAsync()}/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await response.ReadDataAsync()).GetArrayLength());
    }

    [Fact]
    public async Task List_ForUnknownCustomer_Returns404()
    {
        await (await api.Client.GetAsync($"/api/customers/{Guid.NewGuid()}/accounts")).AssertProblemAsync(404, "7232");
    }

    [Fact]
    public async Task Get_UnknownAccount_Returns404()
    {
        await (await api.Client.GetAsync($"/api/accounts/{Guid.NewGuid()}")).AssertProblemAsync(404, "7557");
    }

    [Fact]
    public async Task Close_ClosesAndKeepsAccountVisible()
    {
        var id = await OpenAndGetIdAsync();

        var response = await CloseAsync(id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Closed", (await response.ReadDataAsync()).GetProperty("status").GetString());
        var saved = await (await api.Client.GetAsync($"/api/accounts/{id}")).ReadDataAsync();
        Assert.Equal("Closed", saved.GetProperty("status").GetString());
        Assert.Equal((await response.ReadDataAsync()).GetProperty("closedAt").GetRawText(),
            saved.GetProperty("closedAt").GetRawText());
    }

    [Fact]
    public async Task Close_Twice_Returns409AndKeepsFirstDate()
    {
        var id = await OpenAndGetIdAsync();
        var first = await (await CloseAsync(id)).ReadDataAsync();

        await (await CloseAsync(id)).AssertProblemAsync(409, "7556");

        var saved = await (await api.Client.GetAsync($"/api/accounts/{id}")).ReadDataAsync();
        Assert.Equal(first.GetProperty("closedAt").GetRawText(), saved.GetProperty("closedAt").GetRawText());
    }

    [Fact]
    public async Task Close_Concurrently_SucceedsExactlyOnce()
    {
        for (var round = 0; round < 5; round++)
        {
            var id = await OpenAndGetIdAsync();

            var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => CloseAsync(id)));

            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            foreach (var response in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            {
                await response.AssertProblemAsync(409, "7556");
            }
        }
    }

    [Fact]
    public async Task Close_UnknownAccount_Returns404()
    {
        await (await CloseAsync(Guid.NewGuid())).AssertProblemAsync(404, "7557");
    }
}
