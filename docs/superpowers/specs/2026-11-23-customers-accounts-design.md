# Müşteri ve Hesap — Tasarım

> **Tarih:** 23 Kasım 2026
> **Durum:** Kullanıcı sohbette onayladı; spec ve plan için ayrıca onay beklenmemesini istedi.

## 1. Amaç

NovaBank'ın ilk gerçek ürün özelliği: müşteri oluşturma/görüntüleme ve hesap açma/görüntüleme/kapatma. Bakiye 0'dan başlar, para hareketi yoktur. Proje C#/.NET öğrenme amaçlıdır; dört katman korunur.

## 2. Kararlar

| # | Karar | Kim |
| --- | --- | --- |
| 1 | Kapsam: müşteri + hesap; para hareketi ve login yok | Kullanıcı |
| 2 | Müşteri ad, soyad, benzersiz e-posta (küçük harfle saklanır); TCKN yok | Kullanıcı |
| 3 | TRY, USD, EUR; hesap başına tek ve değişmez para birimi; aynı para biriminde birden çok hesap | Kullanıcı |
| 4 | TR IBAN, sunucu üretir, mod 97, kurgusal banka kodu `99999` | Kullanıcı |
| 5 | Controller → Application servisi → küçük repository arayüzü (kodu Infrastructure'da) | Kullanıcı |
| 6 | Kimlikler `Guid`; para `decimal` / `numeric(18,2)`; zamanlar UTC | Öneri, onaylandı |

## 3. Domain

- `Customer`: Id, FirstName, LastName, Email, CreatedAt.
- `Account`: Id, CustomerId, Iban, Currency, Balance, Status (`Open`/`Closed`), OpenedAt, ClosedAt.
- `Iban`: `TR` + 2 kontrol + `99999` + `0` + 16 rastgele rakam = 26 karakter.
- Domain, Application'daki `BusinessException`'ı bilmez. `Account.Close` zaten kapalıysa `false` döner, servis bunu hataya çevirir.
- Metin alanları baştaki/sondaki boşluktan arındırılır; e-posta `ToLowerInvariant` ile küçültülür.

## 4. HTTP

| Adres | Başarı |
| --- | --- |
| `POST /api/customers` | 201 + Location |
| `GET /api/customers/{id}` | 200 |
| `POST /api/customers/{customerId}/accounts` | 201 + Location |
| `GET /api/customers/{customerId}/accounts` | 200, açılış sırasıyla liste; müşteri yoksa 404 |
| `GET /api/accounts/{id}` | 200 |
| `POST /api/accounts/{id}/close` | 200 |

Müşteri gövdesi: `id, firstName, lastName, email, createdAt`. Hesap gövdesi: `id, customerId, iban, currency, balance, status, openedAt, closedAt`.

| Durum | Kod | HTTP |
| --- | --- | --- |
| Müşteri yok | `7232` | 404 |
| E-posta kayıtlı | `7233` | 409 |
| Hesap yok | `7557` | 404 |
| Hesap zaten kapalı | `7556` | 409 |
| Geçersiz alan / para birimi | `7000` | 400 |

Doğrulama: ad ve soyad zorunlu, en çok 100 karakter; e-posta zorunlu, biçimli, en çok 254; para birimi `TRY`, `USD`, `EUR` (büyük/küçük harf duyarlı, sayı kabul edilmez).

## 5. Kalıcılık

- `customers` (e-posta benzersiz index) ve `accounts` (IBAN benzersiz index, müşteri FK `Restrict`).
- Para birimi ve durum metin olarak saklanır.
- Aynı e-posta yarışında PostgreSQL `23505` hatası `7233`'e çevrilir; 500 dönmez.

## 6. Kapsam dışı

Para hareketi, "bakiyesi olan hesap kapanmaz" kuralı, eşzamanlı bakiye koruması, kur çevirme, sayfalama, login/yetki. Login gelene kadar API internete açılmamalıdır.

## 7. Kabul ölçütleri

Derleme uyarısız; birim testleri ve gerçek PostgreSQL üzerinde endpoint, eşzamanlı e-posta ve migration testleri geçer; e-posta loglara yazılmaz.
