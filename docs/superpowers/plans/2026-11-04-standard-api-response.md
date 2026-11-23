# Standard API Response — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Başarılı JSON cevapları için `ApiResponse<T>` sözleşmesini ve onu üreten `ApiControllerBase` yardımcılarını eklemek; sözleşmeyi kalıcı entegrasyon testleriyle korumak.

**Architecture:** Tüm yeni üretim kodu `NovaBank.Api` içinde. Hatalar mevcut `GlobalExceptionHandler` / `ProblemDetails` ile değişmeden devam eder. Doğrulama, API'yi `WebApplicationFactory<Program>` ile bellekte başlatan yeni bir xUnit projesiyle yapılır; test controller'ı yalnızca test projesinde bulunur.

**Tech Stack:** .NET 8 (`net8.0`), ASP.NET Core, System.Text.Json, xUnit, `Microsoft.AspNetCore.Mvc.Testing` 8.0.x.

**Spec:** `docs/superpowers/specs/2026-11-04-standard-api-response-design.md`

## Global Constraints

- Hedef framework `net8.0`; framework yükseltmesi yok.
- `Microsoft.AspNetCore.Mvc.Testing` sürümü **8.0.x** olmalı (net8 uyumlu); sabit sürüm yazılır, `*` kullanılmaz.
- Application ve Domain'e HTTP bağımlılığı eklenmez.
- `GlobalExceptionHandler.cs` değiştirilmez.
- `/health` düz metin `Healthy` döndürmeye devam eder; Swagger davranışı değişmez.
- Global JSON serializer ayarı değiştirilmez; null gizleme yalnızca `Message` alanında.
- JSON alan adları: `data`, `message`, `traceId`.
- `traceId` = `HttpContext.TraceIdentifier`.
- Production koduna demo/test controller eklenmez.
- Kullanıcının mevcut Türkçe açıklama yorumları korunur.
- **Commit'leri kullanıcı çalıştırır.** Uygulayıcı commit yapmaz; her görev sonunda komutu kullanıcıya verir.

## Review Focus

1. **Türkçe karakterli `message`** (`"Hesap açıldı."`): System.Text.Json varsayılan olarak `ç` gibi kaçış yapar; JSON parse edilince metin aynen geri gelmeli. → Task 2, `Created_ReturnsMessageWithTurkishCharacters`.
2. **`data` içindeki null alt alanlar**: yalnızca `message` gizlenmeli; `data` içindeki null özellikler JSON'da `null` olarak kalmalı. → Task 2, `Ok_KeepsNullPropertiesInsideData`.
3. **Başarılı cevabın Content-Type'ı** `application/json` olmalı, `application/problem+json` değil. → Task 2, `Ok_ReturnsApplicationJson`.
4. **`Location` header'ı gerçekten çalışan adresi göstermeli**: takip edilince `200` dönmeli. → Task 2, `Created_LocationPointsToExistingResource`.
5. **Tanımsız route** sarılmamalı: `404` ve `ApiResponse` gövdesi yok. → Task 1, `UnknownRoute_Returns404WithoutApiResponseBody`.

---

### Task 1: Test projesi ve mevcut davranışı sabitleyen testler

Amaç: Test altyapısını kurmak ve **değişmemesi gereken** davranışları (health, iş hatası, tanımsız route) testle sabitlemek. Bu görevde üretim davranışı değişmez.

**Files:**
- Modify: `src/NovaBank.Api/Program.cs` (dosya sonu)
- Modify: `NovaBank.sln` (test projesi eklenir)
- Create: `tests/NovaBank.Api.IntegrationTests/NovaBank.Api.IntegrationTests.csproj`
- Create: `tests/NovaBank.Api.IntegrationTests/Fixtures/NovaBankApiFactory.cs`
- Create: `tests/NovaBank.Api.IntegrationTests/TestControllers/TestResponsesController.cs`
- Create: `tests/NovaBank.Api.IntegrationTests/ExistingBehaviorTests.cs`

**Interfaces:**
- Produces:
  - `public partial class Program { }` (`Program.cs` sonunda)
  - `public sealed class NovaBankApiFactory : WebApplicationFactory<Program>` — `ConfigureWebHost` içinde `services.AddControllers().AddApplicationPart(typeof(TestResponsesController).Assembly)`.
  - `[Route("test")] public sealed class TestResponsesController : ControllerBase` — Task 2 bunun base class'ını `ApiControllerBase` yapar.
  - Action: `[HttpGet("business-error")] IActionResult BusinessError()` → `throw new BusinessException(ErrorCodes.InsufficientBalance, "Bakiye yetersiz.")`.

- [ ] **Step 1: Test projesini oluştur ve bağla**

```bash
dotnet new xunit -o tests/NovaBank.Api.IntegrationTests -f net8.0
dotnet sln NovaBank.sln add tests/NovaBank.Api.IntegrationTests
dotnet add tests/NovaBank.Api.IntegrationTests reference src/NovaBank.Api
dotnet package search Microsoft.AspNetCore.Mvc.Testing --exact-match
dotnet add tests/NovaBank.Api.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing --version <listedeki en yüksek 8.0.x>
```

Şablonun ürettiği `UnitTest1.cs` silinir. `.csproj` içinde `Nullable` ve `ImplicitUsings` açık olmalı.

- [ ] **Step 2: `Program.cs` sonuna `public partial class Program { }` ekle**

- [ ] **Step 3: `NovaBankApiFactory` ve `TestResponsesController` (yalnızca `business-error` action'ı) oluştur**

- [ ] **Step 4: `ExistingBehaviorTests` yaz** (`IClassFixture<NovaBankApiFactory>`)

```csharp
[Fact] Health_ReturnsPlainTextHealthy()
// GET /health → 200; body == "Healthy"; Content-Type media type == "text/plain"

[Fact] BusinessException_ReturnsProblemDetails()
// GET /test/business-error → 409
// Content-Type media type == "application/problem+json"
// json: status == 409, title == "Bakiye yetersiz.", errorCode == "7555",
//       instance == "/test/business-error", traceId boş değil

[Fact] UnknownRoute_Returns404WithoutApiResponseBody()
// GET /test/does-not-exist → 404; body boş VEYA "data" anahtarı içermiyor
```

- [ ] **Step 5: Testleri çalıştır**

Run: `dotnet test NovaBank.sln`
Expected: `Passed: 3, Failed: 0`. (Bu testler mevcut davranışı belgeler; ilk çalıştırmada geçmeleri beklenir. Biri kalırsa mevcut davranış spec'ten farklıdır — kod değiştirmeden önce dur ve kullanıcıya bildir.)

- [ ] **Step 6: Build kontrolü**

Run: `dotnet build NovaBank.sln`
Expected: `0 Error(s)`

- [ ] **Step 7: Commit (kullanıcı çalıştırır)**

```bash
git add NovaBank.sln src/NovaBank.Api/Program.cs tests/NovaBank.Api.IntegrationTests
git commit -m "test: add API integration test project"
```

---

### Task 2: `ApiResponse<T>` ve `ApiControllerBase`

**Files:**
- Create: `src/NovaBank.Api/Contracts/Responses/ApiResponse.cs`
- Create: `src/NovaBank.Api/Controllers/ApiControllerBase.cs`
- Modify: `tests/NovaBank.Api.IntegrationTests/TestControllers/TestResponsesController.cs`
- Create: `tests/NovaBank.Api.IntegrationTests/ApiResponseTests.cs`

**Interfaces:**
- Consumes: `NovaBankApiFactory`, `TestResponsesController` (Task 1).
- Produces:
  - `namespace NovaBank.Api.Contracts.Responses; public sealed record ApiResponse<T>(T Data, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message, string TraceId);`
  - `namespace NovaBank.Api.Controllers; [ApiController] public abstract class ApiControllerBase : ControllerBase`
    - `protected OkObjectResult OkResponse<T>(T data, string? message = null)`
    - `protected CreatedAtActionResult CreatedResponse<T>(string actionName, object? routeValues, T data, string? message = null)`

- [ ] **Step 1: Test controller'ını genişlet** — base class `ApiControllerBase` olur; aşağıdaki action'lar eklenir (`business-error` korunur):

| Route | Dönüş |
| --- | --- |
| `GET test/single` | `OkResponse(new { id = 1, name = "Test" })` |
| `GET test/with-message` | `OkResponse(new { id = 1 }, "İşlem tamam.")` |
| `GET test/empty-list` | `OkResponse(Array.Empty<int>())` |
| `GET test/null-inside-data` | `OkResponse(new { id = 1, nickname = (string?)null })` |
| `GET test/items/{id:int}` (`Name` yok, action adı `GetItem`) | `OkResponse(new { id })` |
| `POST test/items` | `CreatedResponse(nameof(GetItem), new { id = 42 }, new { id = 42 }, "Hesap açıldı.")` |

- [ ] **Step 2: `ApiResponseTests` yaz** (`IClassFixture<NovaBankApiFactory>`; JSON `JsonDocument` ile okunur)

```csharp
[Fact] Ok_ReturnsDataAndTraceId()
// GET /test/single → 200; data.id == 1; data.name == "Test"; traceId boş değil

[Fact] Ok_WithoutMessage_OmitsMessageKey()
// GET /test/single → root'ta "message" anahtarı YOK (TryGetProperty == false)

[Fact] Ok_WithMessage_ReturnsMessage()
// GET /test/with-message → message == "İşlem tamam."

[Fact] Ok_EmptyList_ReturnsEmptyArray()
// GET /test/empty-list → data.ValueKind == Array; data.GetArrayLength() == 0

[Fact] Ok_KeepsNullPropertiesInsideData()
// GET /test/null-inside-data → data içinde "nickname" anahtarı VAR ve ValueKind == Null

[Fact] Ok_ReturnsApplicationJson()
// GET /test/single → Content-Type media type == "application/json"

[Fact] Created_Returns201WithLocationAndBody()
// POST /test/items → 201; Location header var; data.id == 42; traceId boş değil

[Fact] Created_ReturnsMessageWithTurkishCharacters()
// POST /test/items → parse edilmiş message == "Hesap açıldı."

[Fact] Created_LocationPointsToExistingResource()
// POST /test/items → Location'a GET → 200; data.id == 42
```

- [ ] **Step 3: Testlerin kırmızı olduğunu gör**

Run: `dotnet test NovaBank.sln`
Expected: Derleme hatası — `ApiControllerBase` / `OkResponse` bulunamıyor.

- [ ] **Step 4: `ApiResponse<T>` ve `ApiControllerBase`'i yaz** (Interfaces bloğundaki imzalarla). Her iki yardımcı `TraceId` için `HttpContext.TraceIdentifier` kullanır; `CreatedResponse` `CreatedAtAction(actionName, routeValues, response)` döndürür. Kısa Türkçe açıklama yorumları mevcut dosyaların yoğunluğunda olabilir.

- [ ] **Step 5: Testleri çalıştır**

Run: `dotnet test NovaBank.sln`
Expected: `Passed: 12, Failed: 0` (Task 1'den 3 + bu görevden 9).

- [ ] **Step 6: Değişmemesi gerekenleri kontrol et**

Run: `git diff main --stat -- src/NovaBank.Api/ExceptionHandlers src/NovaBank.Application src/NovaBank.Domain src/NovaBank.Infrastructure`
Expected: Çıktı boş.

- [ ] **Step 7: Commit (kullanıcı çalıştırır)**

```bash
git add src/NovaBank.Api/Contracts src/NovaBank.Api/Controllers tests/NovaBank.Api.IntegrationTests
git commit -m "feat(api): add standard ApiResponse contract"
```
