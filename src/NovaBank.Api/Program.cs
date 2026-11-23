using NovaBank.Api.ExceptionHandlers;
using NovaBank.Api.Extensions;
using NovaBank.Application;
using NovaBank.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Hata yanıtını oluşturacak handler'ı kaydeder.
builder.Services.AddProblemDetails(); // Standart hata yanıtı desteğini ekler.
builder.Services.AddApplication(); // Validator'ları kaydeder.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

// TODO: Swagger ve health check servis kayıtlarını burada ekle.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseExceptionHandler(); // Asıl yakalayan: yakalanmamış exception'ı GlobalExceptionHandler'a iletir.

if (app.Environment.IsDevelopment())  //microsoft kullanım şekli
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// TODO: Swagger middleware'lerini ve health check endpoint'ini burada ekle.

app.MapControllers();

app.MapHealthChecks("/health");
app.Run();

// Entegrasyon testlerinin (WebApplicationFactory<Program>) uygulamayı başlatabilmesi için Program'ı görünür yapar.
public partial class Program { }
