using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaBank.Infrastructure.Persistence;
using Serilog.Core;

namespace NovaBank.Api.IntegrationTests.Fixtures;

// API + migration'ları uygulanmış geçici PostgreSQL.
public sealed class DatabaseApiFixture : IAsyncLifetime
{
    private readonly PostgresFixture _postgres = new();
    private readonly NovaBankApiFactory _baseFactory = new();
    private WebApplicationFactory<Program> _factory = null!;

    public CollectingSink Logs { get; } = new();
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();

        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:NovaBank", _postgres.ConnectionString);
            builder.ConfigureServices(services => services.AddSingleton<ILogEventSink>(Logs));
        });

        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<NovaBankDbContext>().Database.MigrateAsync();
        }

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _baseFactory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
