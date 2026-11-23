# Backend Altyapısı (P2–P7) — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Her görev TDD ile yapılır: önce kırmızı test.

**Goal:** Handoff belgesindeki P2–P7 işlerini küçük, sıralı ve ayrı branch'lerde tamamlamak.

**Architecture:** Mevcut dört katman korunur. HTTP'ye ait her şey `NovaBank.Api` içindedir. Doğrulama kuralları Application'da, veritabanı Infrastructure'da kalır.

**Tech Stack:** .NET 8, FluentValidation 12.2.0, Serilog.AspNetCore 8.0.3, HealthChecks.EntityFrameworkCore 8.0.32, Testcontainers.PostgreSql 4.17.0, xUnit.

**Spec:** Yerel `CLAUDE_PROJECT_HANDOFF.md` bölüm 10 ve 11 (repoya dahil değildir).

## Global Constraints

- P0 kararı (23 Kasım 2026): şimdilik `net8.0`. Destek 10 Kasım 2026'da bitti; geçiş ayrı bir iş olarak açık kalır.
- Application/Domain'e HTTP bağımlılığı yok; production'a demo controller yok.
- Başarı = `ApiResponse<T>`, hata = `ProblemDetails`. `/health` düz metin `Healthy` kalır.
- Kullanıcının Türkçe yorumları korunur. `CLAUDE_PROJECT_HANDOFF.md` commit'lenmez.
- Her görev kendi branch'inde commit + push edilir; branch'ler üst üstedir, `main`'e dokunulmaz.

## Görevler

### Task 1 (P2): FluentValidation — `feat/fluent-validation`
- `ErrorCodes.ValidationFailed = "7000"`; `AddApplication()` validator'ları kaydeder.
- Action içinde görünür `await validator.ValidateAndThrowAsync(request, ct)`.
- `GlobalExceptionHandler`: `ValidationException` → 400 `ValidationProblemDetails`; `errors: { alan: string[] }`, camelCase alan adı.
- Model binding hataları da aynı gövdeyi kullanır; iç tip adı sızmaz.
- Testler: geçerli istek, alan hataları, async kural, geçersiz istekte iş çalışmaz, bozuk JSON, yanlış tip, boş gövde, iç içe alan.

### Task 2 (P3): Serilog — `feat/serilog`
- Seviyeler `Serilog` bölümünden; konsol sink; `UseSerilogRequestLogging`.
- Exception logunun sahibi `GlobalExceptionHandler`: 500 → Error (tek kayıt), iş/doğrulama hatası → stack trace'siz Information.
- Testler: request logu, `TraceId`, tek exception kaydı, sorgu dizesi loglanmaz.

### Task 3 (P4): Health check — `feat/database-health-check`
- `/health` ve `/health/live`: yalnızca API. `/health/ready`: `AddDbContextCheck<NovaBankDbContext>` (etiket `ready`).
- Testler: gerçek PostgreSQL (Testcontainers) → 200; erişilemez → 503 ve ayrıntısız gövde; o sırada live → 200.

### Task 4 (P5): Production — `feat/production-configuration`
- Boş/eksik bağlantı dizesi anlaşılır başlangıç hatası verir.
- Testler: Production'da Swagger 404, 500 gövdesi sızıntısız, bağlantı yoksa başlamaz.

### Task 5 (P6): Kalıcı testler — `test/persistence-and-errors`
- `BusinessException` constructor, hata eşleme tablosu, migration'ın gerçek PostgreSQL'e uygulanması.

### Task 6 (P7): Temizlik — `chore/cleanup-docs`
- Swagger kurulumu extension'a taşınır; eski TODO'lar silinir; README güncellenir.

## Review Focus

1. Türkçe kültürde camelCase (`Id` → `ıd` olmamalı) → Task 1.
2. İç içe/koleksiyon alan adları (`Address.City`, `Items[0].Name`) → Task 1.
3. Beklenmeyen hatada çift Error logu → Task 2.
4. Veritabanı kapalıyken liveness'ın da düşmesi → Task 3.
5. Yalnızca boşluktan oluşan bağlantı dizesi → Task 4.
