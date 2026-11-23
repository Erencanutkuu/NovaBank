# Müşteri ve Hesap — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (Native). Her görev TDD.

**Goal:** Müşteri ve hesap endpoint'lerini PostgreSQL üzerinde çalışır hale getirmek.

**Architecture:** Controller → `CustomerService`/`AccountService` (Application) → `ICustomerRepository`/`IAccountRepository` (Application arayüzü, Infrastructure kodu). Domain hiçbir katmana bağımlı değildir.

**Tech Stack:** .NET 8, EF Core 8.0.32, Npgsql 8.0.11, FluentValidation 12.2.0, xUnit, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-11-23-customers-accounts-design.md`

## Global Constraints

- `net8.0`; yeni production paketi yok. Generic repository, MediatR, AutoMapper yok.
- Application/Domain'de HTTP ve EF bağımlılığı yok.
- `ApiResponse<T>` / `ProblemDetails` sözleşmesi değişmez.
- `CLAUDE_PROJECT_HANDOFF.md` commit'lenmez. Branch: `feat/customers-accounts`; `main`'e dokunulmaz.

## Review Focus

1. Aynı e-posta farklı büyük/küçük harf veya boşlukla → 409 (Task 2).
2. Aynı e-posta aynı anda → tam biri 201, kalanlar 409 (Task 2).
3. Para birimi `"0"`, `"try"`, `"GBP"`, `null` → 400 (Task 3).
4. Guid olmayan id → 404, 500 değil (Task 2).
5. Hesabı iki kez kapatma → ikincisi 409, `closedAt` değişmez (Task 3).

### Task 1: Domain — `tests/NovaBank.Domain.Tests`
- `Iban.Generate() : Iban`, `Iban.IsValid(string?) : bool`, `Iban.Value`.
- `Customer.Create(string firstName, string lastName, string email, DateTimeOffset now)`, `Customer.NormalizeEmail(string)`.
- `Account.Open(Guid customerId, Currency currency, DateTimeOffset now)`, `bool Close(DateTimeOffset now)`.
- [ ] Kırmızı testler → kod → yeşil → commit

### Task 2: Müşteri endpoint'leri
- `ICustomerRepository { AddAsync, FindAsync, EmailExistsAsync, ExistsAsync }`, `CustomerService { CreateAsync, GetAsync }`.
- Hata kodları `CustomerNotFound = "7232"`, `EmailAlreadyExists = "7233"`.
- EF eşlemeleri ve `AddCustomersAndAccounts` migration'ı (iki tablo).
- [ ] Kırmızı testler → kod → yeşil → commit

### Task 3: Hesap endpoint'leri
- `IAccountRepository { AddAsync, FindAsync, ListByCustomerAsync, SaveChangesAsync }`, `AccountService { OpenAsync, GetAsync, ListAsync, CloseAsync }`.
- Hata kodu `AccountNotFound = "7557"`.
- [ ] Kırmızı testler → kod → yeşil → commit

### Task 4: README, uçtan uca çalıştırma, son gözden geçirme
- [ ] README, gerçek çalıştırma, tüm testler, commit, push
