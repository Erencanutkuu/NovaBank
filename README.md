# NovaBank

.NET 8 ASP.NET Core Web API için katmanlı proje iskeleti.

| Proje | Sorumluluk |
| --- | --- |
| NovaBank.Api | HTTP istekleri ve controller'lar. |
| NovaBank.Domain | İş varlıkları ve iş kuralları. Başka projeye bağımlı değildir. |
| NovaBank.Application | Kullanım senaryoları, servis arayüzleri ve DTO'lar. Domain'e bağımlıdır. |
| NovaBank.Infrastructure | Veritabanı erişimi ve dış servis entegrasyonları. Application ve Domain'e bağımlıdır. |

API, Application ve Infrastructure projelerine referans verir. PostgreSQL erişimi EF Core ile Infrastructure katmanında yapılandırılmıştır. Henüz iş varlığı veya endpoint eklenmemiştir.

## EF Core ve migration nedir?

**Entity Framework Core (EF Core)**, C# nesneleri üzerinden veritabanına erişmeyi sağlayan ORM kütüphanesidir. `NovaBankDbContext`, uygulamanın veritabanı oturumudur. PostgreSQL için Npgsql sağlayıcısı kullanılır; Domain katmanı EF Core'a bağımlı değildir.

**Migration**, C# modelindeki şema değişikliklerini veritabanına uygulayan sürümlü bir dosyadır. `Up` değişikliği uygular, `Down` geri alır. Migration oluşturmak dosya üretir; `database update` bu dosyaları veritabanında çalıştırır.

İlk migration `InitialCreate`, `src/NovaBank.Infrastructure/Persistence/Migrations` altındadır. Henüz entity olmadığı için `Up` ve `Down` boştur; uygulandığında EF Core yalnızca `__EFMigrationsHistory` tablosuna başlangıç kaydını ekler. Müşteri ve hesap gibi modeller eklendiğinde yeni migration oluşturulmalıdır.

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
