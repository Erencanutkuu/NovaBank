# React Frontend — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (Native). Her görev TDD.

**Goal:** Mevcut backend özelliklerini güzel ve güvenilir bir ekrandan kullanmak.

**Architecture:** `frontend/` Vite + React + TypeScript. `src/api` fetch katmanı → sayfalar → bileşenler. Geliştirmede Vite `/api`'yi API'ye iletir. E2E testleri gerçek API sürecini, Vite önizlemesini ve geçici PostgreSQL'i başlatır.

**Tech Stack:** React 19, react-router 7 (8, Node 22.22 ister), Vite 8, TypeScript 5.9; xUnit 2.9.3, Microsoft.Playwright 1.65, Testcontainers 4.17.

**Spec:** `docs/superpowers/specs/2026-11-25-react-frontend-design.md`

## Global Constraints

- Çalışma zamanı npm paketi yalnızca `react`, `react-dom`, `react-router`. Düz CSS.
- `NovaBank.sln` içeriği değişmez; E2E ayrı çalışır. Branch: `feat/react-frontend`; az sayıda commit.
- Geliştirme veritabanı testlerde kullanılmaz.

## Review Focus

1. `1.000` → bin; `10.5`, `1,234`, `1.00`, `-5`, boş → hata (Task 3).
2. Butona hızlı çift tıklama → tek hareket (Task 3).
3. Para isteğinde bağlantı kopması → belirsiz sonuç mesajı, tekrar yok (Task 3).
4. Hesap A'nın geç gelen cevabı B ekranında görünmez (Task 3).
5. 390px genişlikte yatay kayma yok (Task 3).

### Task 1: `GET /api/customers` — `tests/NovaBank.Api.IntegrationTests/CustomerTests.cs`
- `ListTransactionsRequest` + validator → ortak `Common/PageRequest(int Page = 1, int PageSize = 20)`, `PageRequestValidator`.
- `ICustomerRepository.ListAsync(skip, take, ct)`, `CountAsync(ct)`; `CustomerService.ListAsync(PageRequest, ct) : PagedResult<CustomerDto>`; sıra: `CreatedAt` azalan, sonra `Id`.
- [ ] Kırmızı → yeşil → commit

### Task 2: E2E altyapısı ve müşteriler sayfası
- `AppFixture`: Postgres, API süreci, `npm run build` + `vite preview` (`NOVABANK_API` ile hedef), Chromium.
- [ ] Kırmızı → yeşil

### Task 3: Müşteri detayı: hesaplar, para işlemleri, hareketler
- `parseAmount(text) : number | null`, `formatMoney`, `formatIban`, `formatDate`.
- [ ] Kırmızı → yeşil

### Task 4: Görsel kontrol (masaüstü + telefon ekran görüntüsü), README, son gözden geçirme
- [ ] Ekran görüntüleri, tüm testler, commit, push
