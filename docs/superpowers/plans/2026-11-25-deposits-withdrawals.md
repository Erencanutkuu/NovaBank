# Para Yatırma ve Çekme — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (Native). Her görev TDD.

**Goal:** Hesaba para yatırma/çekme ve hareket listesini eşzamanlılığa dayanıklı biçimde çalıştırmak.

**Architecture:** `TransactionsController` → `TransactionService` → `IUnitOfWork` + `IAccountRepository.FindForUpdateAsync` + `ITransactionRepository`. Bakiye ve hareket aynı veritabanı işleminde yazılır.

**Tech Stack:** .NET 8, EF Core 8.0.32, Npgsql 8.0.11, FluentValidation 12.2.0, xUnit, Testcontainers.

**Spec:** `docs/superpowers/specs/2026-11-25-deposits-withdrawals-design.md`

## Global Constraints

- `net8.0`; yeni paket yok. Generic repository, MediatR, AutoMapper yok.
- Application/Domain'de HTTP ve EF bağımlılığı yok. Cevap sözleşmesi değişmez.
- Branch: `feat/deposits-withdrawals`; `main`'e dokunulmaz; az sayıda commit.

## Review Focus

1. Aynı anda 20 çekme, bakiye yalnızca bir kısmına yeter → fazlası 409, bakiye eksiye düşmez (Task 2).
2. Aynı anda 20 yatırma → hepsi 200, hiçbiri kaybolmaz (Task 2).
3. Kapatma ile yatırma yarışı → kapalı hesapta para kalmaz (Task 3).
4. Tutar `0`, eksi, 3 ondalık, sınırın üstü, `null`, aşırı büyük sayı → 400, 500 değil (Task 2).
5. `page=abc`, `page=0`, dev `page` → Türkçe mesajla 400 (Task 2).

### Task 1: Domain — `tests/NovaBank.Domain.Tests/AccountTests.cs`
- `AccountTransaction` / `AccountTransactionType { Deposit, Withdrawal }`.
- `AccountTransaction Account.Deposit(decimal, DateTimeOffset)`, `AccountTransaction? Account.Withdraw(decimal, DateTimeOffset)`, `const decimal Account.MaxTransactionAmount = 1_000_000m`, `const int Account.AmountScale = 2`.
- `Close` bakiye varken `InvalidOperationException`.
- [ ] Kırmızı testler → kod → yeşil

### Task 2: Yatırma, çekme, listeleme
- `IUnitOfWork.BeginAsync(ct) : IUnitOfWorkTransaction`; `IUnitOfWorkTransaction : IAsyncDisposable { CommitAsync(ct) }` (kaydeder ve onaylar).
- `IAccountRepository.FindForUpdateAsync(id, ct)`, `ITransactionRepository { Add, ListAsync(accountId, skip, take, ct), CountAsync }`.
- `TransactionService { DepositAsync, WithdrawAsync, ListAsync }`, `PagedResult<T>`, `MoveMoneyRequest(decimal Amount)`, `ListTransactionsRequest(Page, PageSize)`.
- Migration `AddAccountTransactions`: tablo, CHECK'ler, değişikliği engelleyen tetikleyici.
- [ ] Kırmızı testler → kod → yeşil

### Task 3: Kapatma kuralı
- `ErrorCodes.AccountHasBalance = "7558"` → 409. `CloseAsync` kilitle çalışır; `TrySaveChangesAsync` kalkar.
- [ ] Kırmızı testler → kod → yeşil → commit

### Task 4: README, uçtan uca çalıştırma, son gözden geçirme
- [ ] README, migration, gerçek çalıştırma, tüm testler, push
