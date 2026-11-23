using NovaBank.Api.ExceptionHandlers;
using NovaBank.Api.Extensions;
using NovaBank.Application;
using NovaBank.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiLogging(builder.Configuration); // Serilog
builder.Services.AddApiControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Hata yanıtını oluşturacak handler'ı kaydeder.
builder.Services.AddProblemDetails(); // Standart hata yanıtı desteğini ekler.
builder.Services.AddApplication(); // Validator'ları kaydeder.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiHealthChecks();
builder.Services.AddApiDocumentation(); // Swagger
var app = builder.Build();

app.UseApiRequestLogging(); // En dışta: hata cevabının son status kodunu da görür.
app.UseExceptionHandler(); // Asıl yakalayan: yakalanmamış exception'ı GlobalExceptionHandler'a iletir.

app.UseApiDocumentation(); // Swagger yalnızca Development'ta açılır.

app.MapControllers();

app.MapApiHealthChecks();
app.Run();

// Entegrasyon testlerinin (WebApplicationFactory<Program>) uygulamayı başlatabilmesi için Program'ı görünür yapar.
public partial class Program { }
