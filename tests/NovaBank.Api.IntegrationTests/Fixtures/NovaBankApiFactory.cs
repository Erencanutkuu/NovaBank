using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NovaBank.Api.IntegrationTests.TestControllers;

namespace NovaBank.Api.IntegrationTests.Fixtures;

// API'yi bellekte başlatır; gerçek port açılmaz.
public sealed class NovaBankApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Test assembly'sindeki controller'lar varsayılan olarak keşfedilmez; burada eklenir.
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(TestResponsesController).Assembly));
    }
}
