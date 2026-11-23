namespace NovaBank.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())  //microsoft kullanım şekli
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }
}
