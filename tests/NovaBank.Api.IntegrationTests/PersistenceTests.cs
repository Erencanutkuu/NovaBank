using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaBank.Api.IntegrationTests.Fixtures;
using NovaBank.Infrastructure.Persistence;

namespace NovaBank.Api.IntegrationTests;

// İş entity'si henüz yok; migration zincirinin gerçek PostgreSQL'de çalıştığını doğrular.
public sealed class PersistenceTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>, IDisposable
{
    private readonly NovaBankApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Migrations_ApplyToEmptyDatabase()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:NovaBank", postgres.ConnectionString));
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<NovaBankDbContext>().Database;

        // Uygulama açılışta migration çalıştırmaz.
        Assert.Contains("20261019191031_InitialCreate", await database.GetPendingMigrationsAsync());

        await database.MigrateAsync();

        Assert.Empty(await database.GetPendingMigrationsAsync());
        Assert.Contains("20261019191031_InitialCreate", await database.GetAppliedMigrationsAsync());
    }
}
