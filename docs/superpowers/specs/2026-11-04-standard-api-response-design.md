# Standard API Response — Tasarım

> **Tarih:** 4 Kasım 2026
> **İlgili görev:** Handoff belgesi P1 — Standard API Response
> **Durum:** Tasarım onaylandı, uygulanmadı.

## 1. Amaç

Başarılı JSON cevapları için tek ve öngörülebilir bir gövde biçimi tanımlamak. API'yi ileride bir frontend/mobil istemci kullanacak; yapı sektördeki yaygın kalıplara (ör. Open Banking `Data`/`Meta` ayrımı, RFC 9457 `ProblemDetails`) yakın olmalı.

## 2. Alınan kararlar

| # | Karar | Gerekçe |
| --- | --- | --- |
| 1 | Başarılı ve hatalı cevaplar **ayrı** biçimde kalır. Hatalar mevcut `ProblemDetails` ile döner. | Standart; mevcut `GlobalExceptionHandler` değişmez; istemci önce HTTP koduna bakar. |
| 2 | Başarı gövdesi: `data` + `message` (isteğe bağlı) + `traceId`, hepsi üst seviyede. | `success` her zaman `true` olacağından gereksiz. `traceId` hata gövdesinde de üst seviyede; iki biçim tutarlı. |
| 3 | Liste cevabı `data: [...]`. Sayfalama **şimdi yok**. | Liste endpoint'i yok; sayfalama ilk gerçek liste endpoint'inde (muhtemelen işlem geçmişi) tasarlanacak. |
| 4 | Cevaplar ortak `ApiControllerBase` içindeki yardımcı metotlarla üretilir. | `traceId` otomatik dolar; okunması kolay; otomatik sarmalayan filter gizli davranış yaratır. |
| 5 | Doğrulama, küçük ve kalıcı bir entegrasyon test projesiyle yapılır. | Production'a demo controller eklenmez; sözleşme kalıcı olarak korunur. |

## 3. Cevap sözleşmesi

Başarılı tek nesne:

```json
{
  "data": { "id": 1, "balance": 1500.00 },
  "traceId": "0HN7ABC:00000001"
}
```

Mesajlı oluşturma (`201`, `Location` header'ı ile):

```json
{
  "data": { "id": 1 },
  "message": "Hesap açıldı.",
  "traceId": "0HN7ABC:00000002"
}
```

Kurallar:

- `message` null ise JSON'a **yazılmaz**.
- `data` hiçbir zaman null değildir. Bulunamayan kayıt için `BusinessException` (→ `404` `ProblemDetails`) kullanılır.
- Boş liste `"data": []` olarak döner, null olarak değil.
- `traceId`, `HttpContext.TraceIdentifier` değeridir — `GlobalExceptionHandler` ile aynı kaynak.
- Alan adları camelCase'dir (ASP.NET Core varsayılanı).

| Durum | HTTP | Gövde |
| --- | --- | --- |
| Tek nesne | 200 | `ApiResponse<T>` |
| Liste | 200 | `ApiResponse<IReadOnlyList<T>>` |
| Oluşturma | 201 + `Location` | `ApiResponse<T>` |
| Gövdesiz işlem | 204 | Yok (`NoContent()`) |
| İş hatası / beklenmeyen hata | 4xx / 500 | Mevcut `ProblemDetails` — değişmez |
| `/health` | 200 | Düz metin `Healthy` — değişmez |
| Swagger | — | Değişmez |

## 4. Bileşenler

Tümü `NovaBank.Api` içinde. Application/Domain'e HTTP bağımlılığı eklenmez.

### 4.1. `src/NovaBank.Api/Contracts/Responses/ApiResponse.cs` (yeni)

```csharp
public sealed record ApiResponse<T>(
    T Data,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message,
    string TraceId);
```

Null gizleme yalnızca `Message` alanına uygulanır; global serializer ayarı değiştirilmez.

### 4.2. `src/NovaBank.Api/Controllers/ApiControllerBase.cs` (yeni)

```csharp
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected OkObjectResult OkResponse<T>(T data, string? message = null);

    protected CreatedAtActionResult CreatedResponse<T>(
        string actionName, object? routeValues, T data, string? message = null);
}
```

- Her iki metot `ApiResponse<T>` oluşturur ve `TraceId`'yi `HttpContext.TraceIdentifier` ile doldurur.
- `204` için yardımcı yok; `NoContent()` kullanılır.

### 4.3. `src/NovaBank.Api/Program.cs` (değişiklik)

Dosya sonuna test erişimi için:

```csharp
public partial class Program { }
```

Uygulama davranışı değişmez.

## 5. Doğrulama: entegrasyon test projesi

Yeni proje: `tests/NovaBank.Api.IntegrationTests` (xUnit, `net8.0`), solution'a eklenir.

- `Microsoft.AspNetCore.Mvc.Testing` — .NET 8 ile uyumlu `8.0.x` sürümü.
- `WebApplicationFactory<Program>` API'yi bellekte başlatır. Varsayılan ortam `Development` olduğu için bağlantı dizesi `appsettings.Development.json`'dan okunur; testler veritabanına bağlanmaz (`AddDbContext` kaydı bağlantı açmaz).
- `TestResponsesController : ApiControllerBase` **yalnızca test projesinde** bulunur.

Test senaryoları:

| # | Senaryo | Beklenti |
| --- | --- | --- |
| 1 | Tek nesne | `200`, `data` dolu, `traceId` dolu |
| 2 | Mesajsız cevap | JSON'da `message` anahtarı **yok** |
| 3 | Mesajlı cevap | `message` verilen değer |
| 4 | Boş liste | `200`, `data` = `[]` |
| 5 | Oluşturma | `201`, `Location` header'ı var, gövde `ApiResponse` |
| 6 | `BusinessException` fırlatan action | `ProblemDetails` (`application/problem+json`), `errorCode` ve `traceId` mevcut |
| 7 | `/health` | `200`, düz metin `Healthy` |

Test controller'ının uygulama tarafından keşfedilmesi için test host'unda `AddApplicationPart(typeof(TestResponsesController).Assembly)` kullanılır.

## 6. Kapsam dışı

- Sayfalama (`PagedResponse`) — ilk liste endpoint'iyle.
- Validation hata biçimi — P2 (FluentValidation).
- `UseStatusCodePages` ile boş hata gövdelerini doldurma.
- PostgreSQL entegrasyon testleri ve test veritabanı — P6'nın geri kalanı.
- Gerçek iş controller'ları.
- .NET 10 geçişi — P0, ayrı karar.

## 7. Kabul ölçütleri

- `dotnet build NovaBank.sln` 0 hata.
- `dotnet test NovaBank.sln` çıktısında 7 testin keşfedilip geçtiği görülür.
- `GlobalExceptionHandler.cs`, `/health` ve Swagger davranışı değişmemiştir.
