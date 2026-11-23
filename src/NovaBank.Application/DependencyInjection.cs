using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NovaBank.Application.Accounts;
using NovaBank.Application.Customers;

namespace NovaBank.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Bu projedeki tüm AbstractValidator<T> sınıflarını IValidator<T> olarak kaydeder.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<CustomerService>();
        services.AddScoped<AccountService>();

        return services;
    }
}
