using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using NovaBank.Api.IntegrationTests.Fixtures;

namespace NovaBank.Api.IntegrationTests;

public sealed class EnvironmentConfigurationTests : IDisposable
{
    private const string SomeConnection = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x";

    private readonly NovaBankApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient(string environment, string? connectionString) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            if (connectionString is not null)
            {
                builder.UseSetting("ConnectionStrings:NovaBank", connectionString);
            }
        }).CreateClient();

    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task Swagger_IsOnlyAvailableInDevelopment(string environment, HttpStatusCode expected)
    {
        var client = CreateClient(environment, SomeConnection);

        Assert.Equal(expected, (await client.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(expected, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task UnexpectedError_DoesNotLeakDetails(string environment)
    {
        var response = await CreateClient(environment, SomeConnection).GetAsync("/test/unexpected-error");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("gizli-ic-ayrinti", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("9000", json.RootElement.GetProperty("errorCode").GetString());
    }

    // null: ayar hiç yok. Geliştirme bağlantısının Production'a taşınmadığını da gösterir.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Production_WithoutConnectionString_FailsAtStartupWithClearMessage(string? connectionString)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateClient("Production", connectionString));

        Assert.Equal("ConnectionStrings:NovaBank bağlantı ayarı bulunamadı.", exception.Message);
    }
}
