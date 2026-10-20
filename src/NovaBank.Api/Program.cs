using NovaBank.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

// TODO: Swagger ve health check servis kayıtlarını burada ekle.

var app = builder.Build();

// TODO: Swagger middleware'lerini ve health check endpoint'ini burada ekle.

app.MapControllers();

app.MapHealthChecks("/health");
app.Run();
