using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaBank.Infrastructure.Persistence;

namespace NovaBank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NovaBank")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:NovaBank bağlantı ayarı bulunamadı.");

        services.AddDbContext<NovaBankDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
