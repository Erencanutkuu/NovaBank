using System.Net;
using Microsoft.AspNetCore.Hosting;
using NovaBank.Api.IntegrationTests.Fixtures;

namespace NovaBank.Api.IntegrationTests;

public sealed class HealthCheckTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>, IDisposable
{
    // 1 numaralı portta PostgreSQL yok: bağlantı hemen reddedilir.
    private const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=novabank;Username=gizli-kullanici;Password=gizli-parola;Timeout=2";

    private readonly NovaBankApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient(string connectionString) =>
        _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:NovaBank", connectionString)).CreateClient();

    [Fact]
    public async Task Ready_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        var response = await CreateClient(postgres.ConnectionString).GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_WhenDatabaseIsUnreachable_Returns503WithoutDetails()
    {
        var response = await CreateClient(UnreachableDatabase).GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    public async Task Liveness_DoesNotDependOnDatabase(string path)
    {
        var response = await CreateClient(UnreachableDatabase).GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
