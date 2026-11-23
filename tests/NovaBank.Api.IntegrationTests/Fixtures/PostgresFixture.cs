using Testcontainers.PostgreSql;

namespace NovaBank.Api.IntegrationTests.Fixtures;

// Test için geçici bir PostgreSQL container'ı açar; geliştirme veritabanına dokunmaz. Docker çalışıyor olmalı.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
