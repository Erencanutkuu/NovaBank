# NovaBank

.NET 8 ASP.NET Core Web API için katmanlı proje iskeleti.

| Proje | Sorumluluk |
| --- | --- |
| NovaBank.Api | HTTP istekleri ve controller'lar. |
| NovaBank.Domain | İş varlıkları ve iş kuralları. Başka projeye bağımlı değildir. |
| NovaBank.Application | Kullanım senaryoları, servis arayüzleri ve DTO'lar. Domain'e bağımlıdır. |
| NovaBank.Infrastructure | Veritabanı erişimi ve dış servis entegrasyonları. Application ve Domain'e bağımlıdır. |

API, Application ve Infrastructure projelerine referans verir. PostgreSQL erişimi EF Core ile Infrastructure katmanında yapılandırılmıştır. Müşteri, hesap ve para yatırma/çekme endpoint'leri vardır; havale ve login henüz yoktur.

> **Uyarı:** Kimlik doğrulama olmadığı için API yalnızca yerelde çalıştırılmalı, internete açılmamalıdır.

## EF Core ve migration nedir?

**Entity Framework Core (EF Core)**, C# nesneleri üzerinden veritabanına erişmeyi sağlayan ORM kütüphanesidir. `NovaBankDbContext`, uygulamanın veritabanı oturumudur. PostgreSQL için Npgsql sağlayıcısı kullanılır; Domain katmanı EF Core'a bağımlı değildir.

**Migration**, C# modelindeki şema değişikliklerini veritabanına uygulayan sürümlü bir dosyadır. `Up` değişikliği uygular, `Down` geri alır. Migration oluşturmak dosya üretir; `database update` bu dosyaları veritabanında çalıştırır.

İlk migration `InitialCreate`, `src/NovaBank.Infrastructure/Persistence/Migrations` altındadır. O sırada entity olmadığı için `Up` ve `Down` boştur. `AddCustomersAndAccounts` ise `customers` ve `accounts`, `AddAccountTransactions` ise `account_transactions` tablosunu oluşturur.

## Yerel kurulum

.NET 8 SDK ve çalışan Docker Desktop gerekir. Komutları repository kökünde çalıştır:

```sh
dotnet tool restore
dotnet restore NovaBank.sln
docker compose up -d --wait

export ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update --project src/NovaBank.Infrastructure --startup-project src/NovaBank.Api

dotnet build NovaBank.sln
dotnet run --project src/NovaBank.Api
```

Docker Compose, PostgreSQL 16'yı `localhost:5433` üzerinde başlatır. Veriler `postgres_data` volume'unda saklanır. `appsettings.Development.json` içindeki `novabank` kullanıcısı ve `novabank_dev` parolası yalnızca yerel geliştirme içindir. Başka bir PostgreSQL kullanıyorsan bağlantıyı ortam değişkeniyle değiştirebilirsin:

```sh
export ConnectionStrings__NovaBank='Host=localhost;Port=5433;Database=novabank;Username=YOUR_USER;Password=YOUR_PASSWORD'
```

Development dışındaki ortamlarda `ConnectionStrings__NovaBank` veya eşdeğer bir güvenli yapılandırma sağlanmalıdır. API başlatılırken migration otomatik uygulanmaz; yukarıdaki `database update` komutuyla açıkça uygulanır.

## Adresler

| Adres | Açıklama |
| --- | --- |
| `/health`, `/health/live` | API ayakta mı? Veritabanını kontrol etmez. |
| `/health/ready` | PostgreSQL'e erişilebiliyor mu? Erişilemezse `503`. |
| `/swagger/index.html` | Yalnızca Development ortamında. |
| `POST /api/customers` | Müşteri oluşturur: `firstName`, `lastName`, `email`. |
| `GET /api/customers` | Müşterileri en yeniden eskiye listeler: `page`, `pageSize`. |
| `GET /api/customers/{id}` | Müşteriyi getirir. |
| `POST /api/customers/{customerId}/accounts` | Hesap açar: `currency` = `TRY`, `USD` veya `EUR`. |
| `GET /api/customers/{customerId}/accounts` | Müşterinin hesaplarını listeler. |
| `GET /api/accounts/{id}` | Hesabı getirir. |
| `POST /api/accounts/{id}/close` | Hesabı kapatır; bakiye 0 olmalıdır, geri açılamaz. |
| `POST /api/accounts/{id}/deposit` | Para yatırır: `amount` (0'dan büyük, en çok 1.000.000, en çok 2 ondalık). |
| `POST /api/accounts/{id}/withdraw` | Para çeker: `amount`. |
| `GET /api/accounts/{id}/transactions` | Hareketleri en yeniden eskiye listeler: `page` (1), `pageSize` (20, en çok 100). |

## Cevap ve hata biçimi

- Başarı: `{ data, message?, traceId }`. Controller'lar `ApiControllerBase` yardımcılarını kullanır.
- Hata: `ProblemDetails` + `errorCode` + `traceId`.
- Doğrulama hatası: `400`, `errorCode: "7000"` ve `errors: { "alan": ["mesaj"] }`. Bozuk JSON ve yanlış tipli alanlar da aynı gövdeyle döner.

| Kod | HTTP | Anlamı |
| --- | --- | --- |
| `7000` | 400 | Doğrulama hatası |
| `7232` | 404 | Müşteri bulunamadı |
| `7233` | 409 | E-posta kayıtlı |
| `7555` | 409 | Bakiye yetersiz |
| `7556` | 409 | Hesap kapalı |
| `7557` | 404 | Hesap bulunamadı |
| `7558` | 409 | Bakiyesi olan hesap kapatılamaz |
| `9000` | 500 | Beklenmeyen hata |

IBAN'lar kurgusal `99999` banka koduyla üretilir; gerçek bir bankaya ait değildir.

Yeni validator'ı Application'a `AbstractValidator<T>` olarak ekle; `AddApplication` otomatik kaydeder. Action içinde iş işleminden önce çağır:

```csharp
await validator.ValidateAndThrowAsync(request, cancellationToken);
```

## Para hareketleri

Hesabın bakiyesi ve hareket kaydı tek veritabanı işleminde yazılır. Hesabı değiştiren her işlem `IUnitOfWork.BeginAsync` ile başlar ve hesabı `FindForUpdateAsync` ile kilitleyerek okur; aynı hesaba aynı anda gelen istekler sırayla çalışır. `account_transactions` satırları değiştirilemez ve silinemez (veritabanı tetikleyicisi).

Tekrar isteği koruması henüz yoktur: istemci aynı isteği iki kez gönderirse iki hareket oluşur.

## Loglama

Serilog ayarları `appsettings.json` içindeki `Serilog` bölümündedir. Her istek için method, path, status ve süre yazılır; `RequestId`, cevaptaki `traceId` ile aynıdır. Sorgu dizesi, gövde ve header'lar loglanmaz. Beklenmeyen hatayı `GlobalExceptionHandler` bir kez Error olarak loglar; iş ve doğrulama hataları stack trace olmadan Information seviyesindedir.

## Testler

```sh
dotnet test NovaBank.sln
```

Docker çalışmalıdır: PostgreSQL testleri geçici bir container açar, geliştirme veritabanına dokunmaz.

## Production ortamında çalıştırma

```sh
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__NovaBank='Host=...;Database=...;Username=...;Password=...'
dotnet run --project src/NovaBank.Api --no-launch-profile
```

Bağlantı ayarı yoksa veya boşsa uygulama başlamaz. Bu komut ortam davranışını kontrol eder; HTTPS, secret yönetimi, `AllowedHosts` ve yayınlama düzeni henüz kurulmamıştır.

## Model değiştirince

Entity sınıflarını Domain'e, `IEntityTypeConfiguration<T>` eşlemelerini Infrastructure'a ekle. Context bu eşlemeleri assembly üzerinden otomatik yükler. Ardından anlamlı bir adla yeni migration oluştur ve uygula:

```sh
export ASPNETCORE_ENVIRONMENT=Development
dotnet ef migrations add AddAccounts --project src/NovaBank.Infrastructure --startup-project src/NovaBank.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/NovaBank.Infrastructure --startup-project src/NovaBank.Api
```

Oluşturulan migration dosyalarını inceleyip kaynak kontrolüne dahil et. Model ile son migration'ın uyumunu veritabanına bağlanmadan kontrol etmek için:

```sh
dotnet ef migrations has-pending-model-changes --project src/NovaBank.Infrastructure --startup-project src/NovaBank.Api -- --environment Development
```
