# Para Yatırma ve Çekme — Tasarım

> **Tarih:** 25 Kasım 2026
> **Durum:** Kayıt modelini (A) kullanıcı seçti. Kullanıcı bu iş için tasarım ve plan onayının beklenmemesini istedi; "Öneri" satırları ayrıca onaylanmadı.

## 1. Amaç

Hesaba para yatırmak, hesaptan para çekmek ve hareketleri listelemek. Bakiye ile hareket kaydı hiçbir durumda ayrışmamalı, bakiye eksiye düşmemeli.

## 2. Kararlar

| # | Karar | Kim |
| --- | --- | --- |
| 1 | Hesapta bakiye + değiştirilemez hareket tablosu; ikisi tek veritabanı işleminde yazılır | Kullanıcı |
| 2 | Eşzamanlılık: hesap satırı `SELECT ... FOR UPDATE` ile kilitlenir. Sıradaki istek bekler, hata almaz | Öneri |
| 3 | Tutar: 0'dan büyük, en çok 2 ondalık, işlem başına en çok 1.000.000 | Öneri |
| 4 | Bakiyesi 0 olmayan hesap kapatılamaz: `7558`, 409 | Kullanıcı (kapsam), Öneri (kod) |
| 5 | Hareketler sayfalı, en yeni önce | Öneri |
| 6 | Hareket satırını değiştirmeyi ve silmeyi veritabanı tetikleyicisi de engeller | Öneri |

Elenenler: iyimser kilit + yeniden deneme (yoğunlukta geçerli istekleri reddeder), çift taraflı kayıt (kullanıcı A'yı seçti).

## 3. Domain

- `AccountTransaction`: Id, AccountId, Type (`Deposit`/`Withdrawal`), Amount (hep artı), BalanceAfter, CreatedAt.
- `Account.Deposit(amount, now)` hareketi döner. `Account.Withdraw(amount, now)` bakiye yetmezse hiçbir şeyi değiştirmeden `null` döner.
- Kapalı hesapta işlem, bakiyeli hesabı kapatma ve geçersiz tutar programlama hatasıdır (exception). Servis bunları önceden kontrol edip iş hatasına çevirir.

## 4. HTTP

| Adres | Başarı |
| --- | --- |
| `POST /api/accounts/{id}/deposit` — `{ amount }` | 200, hareket |
| `POST /api/accounts/{id}/withdraw` — `{ amount }` | 200, hareket |
| `GET /api/accounts/{id}/transactions?page=1&pageSize=20` | 200, `{ items, page, pageSize, totalCount }` |

Hareket gövdesi: `id, accountId, type, amount, balanceAfter, createdAt`. `pageSize` 1–100, `page` 1–1.000.000.

| Durum | Kod | HTTP |
| --- | --- | --- |
| Geçersiz tutar veya sayfa | `7000` | 400 |
| Bakiye yetersiz | `7555` | 409 |
| Hesap kapalı | `7556` | 409 |
| Hesap yok | `7557` | 404 |
| Bakiyesi olan hesap kapatılıyor | `7558` | 409 |

## 5. Kalıcılık

- `account_transactions`: hesap FK `Restrict`; ekleme sırasını veren `sequence` (identity); `(account_id, sequence)` benzersiz index.
- Sıralama saate değil `sequence` değerine göredir; saat geri alınsa da sıra bozulmaz.
- CHECK: `accounts.balance >= 0`, `amount > 0`, `balance_after >= 0`.
- Application `IUnitOfWork` ile işlem başlatır; EF ayrıntısı Infrastructure'da kalır. Kapatma da aynı kilidi kullanır.
- `xmin` ikinci emniyet olarak kalır.

## 6. Kapsam dışı

Havale, kur çevirme, açıklama alanı, günlük limit, hareket iptali, login/yetki ve tekrar isteği koruması (`Idempotency-Key`). Son madde yüzünden istemci aynı isteği iki kez gönderirse iki hareket oluşur; API internete açılmadan önce ele alınmalıdır.

## 7. Kabul ölçütleri

Derleme uyarısız; gerçek PostgreSQL'de eşzamanlı yatırma, çekme ve kapatma testleri geçer; her hesapta bakiye, hareketlerin toplamına eşittir.
