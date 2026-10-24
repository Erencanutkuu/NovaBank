using NovaBank.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

// TODO: Swagger ve health check servis kayıtlarını burada ekle.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// TODO: Swagger middleware'lerini ve health check endpoint'ini burada ekle.

app.MapControllers();

app.MapHealthChecks("/health");
app.Run();
