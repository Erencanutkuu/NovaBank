using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaBank.Application.Customers;
using NovaBank.Infrastructure.Persistence;
using NovaBank.Infrastructure.Persistence.Repositories;

namespace NovaBank.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NovaBank");

        // Boş değer de eksik sayılır; hata ilk istekte değil, başlangıçta görülür.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:NovaBank bağlantı ayarı bulunamadı.");
        }

        services.AddDbContext<NovaBankDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        return services;
    }
}
