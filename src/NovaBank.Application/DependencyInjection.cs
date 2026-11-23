using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace NovaBank.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Bu projedeki tüm AbstractValidator<T> sınıflarını IValidator<T> olarak kaydeder.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
