# React Frontend — Tasarım

> **Tarih:** 25 Kasım 2026
> **Durum:** Kullanıcı 25 Kasım 2026'da sohbette onayladı.

## 1. Amaç

Mevcut backend özelliklerini Swagger yerine bir ekrandan kullanmak ve denemek: müşteri, hesap, para yatırma/çekme, hareketler.

## 2. Kararlar

| # | Karar | Kim |
| --- | --- | --- |
| 1 | React + TypeScript | Kullanıcı |
| 2 | Testler xUnit | Kullanıcı |
| 3 | Yerleşim: hesap kartları üstte, seçilen hesabın detayı altta | Kullanıcı |
| 4 | Müşteri listeden seçilir; backend'e `GET /api/customers` eklenir | Kullanıcı |
| 5 | Vite; ek paket yalnızca `react-router`. Arayüz kütüphanesi yok, düz CSS | Öneri |
| 6 | Geliştirmede Vite `/api` isteklerini `localhost:5080`'e iletir; CORS açılmaz | Öneri |
| 7 | Ekran testleri xUnit + Playwright for .NET, gerçek tarayıcı + gerçek API + geçici PostgreSQL | Öneri |
| 8 | E2E projesi `NovaBank.sln` dışında; `dotnet test` Node ve tarayıcı istemeden çalışmaya devam eder | Öneri |

## 3. Klasörler

```
frontend/                      React uygulaması
  src/api/                     backend çağrıları ve tipler
  src/pages/                   CustomersPage, CustomerPage
  src/components/              AccountCard, TransactionList, MoneyDialog ...
  src/format.ts                para, tarih ve IBAN biçimi; tutar okuma
tests/NovaBank.E2E.Tests/      xUnit + Playwright
```

## 4. Ekranlar

**Müşteriler (`/`):** sayfalı liste (en yeni önce), müşteri ekleme formu (ad, soyad, e-posta). Müşteriye tıklayınca detayı açılır.

**Müşteri (`/customers/:id`):**
- Hesap kartları: para birimi, durum, bakiye, IBAN. "Hesap aç": TRY, USD, EUR.
- Seçilen hesap: Yatır, Çek, Hesabı kapat; altında sayfalı hareket listesi.
- Kapatma onay ister, çünkü geri alınamaz. Kapalı hesapta Yatır, Çek ve Kapat pasiftir.

## 5. Davranış

- **Tutar:** Türkçe yazılır: `1.250,50`. Virgül ondalıktır; nokta yalnızca doğru yerdeyse binlik sayılır. `10.5` gibi belirsiz yazımlar tahmin edilmez, hata verilir.
- **Çift tıklama:** istek sürerken buton pasiftir. Bu, sunucudaki çift istek korumasının yerini tutmaz.
- **Bağlantı koparsa:** istek kendiliğinden tekrarlanmaz. Para işleminde "Sonuç belirsiz, hareketleri kontrol et" yazılır.
- **Hatalar:** backend'in mesajı ekranda gösterilir; alan hataları ilgili alanın altında. API kapalıysa "Sunucuya ulaşılamıyor".
- **Hesap değiştirme:** eski hesaptan geç gelen cevap yeni hesabın ekranına yazılmaz.
- Telefonda kullanılabilir, klavyeyle gezilebilir, form alanları etiketlidir.

## 6. Backend değişikliği

`GET /api/customers?page=1&pageSize=20` → `{ items, page, pageSize, totalCount }`, en yeni önce. Sayfa kuralları hareket listesiyle aynıdır (`7000`, 400). Müşterileri kişisel veriyle listeler; login gelince yetkiye bağlanmalıdır. Migration gerekmez.

## 7. Testler

- **Backend:** `CustomerTests` içine liste, sıra, sayfalama ve geçersiz sayfa testleri.
- **Ekran (Playwright):** müşteri ekleme; aynı e-posta hatası; hesap açma; 1.000 yatır, 250 çek, 750 gör; yetersiz bakiye; bakiyeli hesabı kapatamama; hesap kapatma; tutar yazımları; çift tıklamada tek hareket; sayfalama; API kapalıyken mesaj; telefon genişliği.
- E2E kendi geçici veritabanını kullanır, geliştirme verisine dokunmaz.

## 8. Kapsam dışı

Login, havale, sunucu tarafı çift istek koruması, müşteri arama/düzenleme/silme, karanlık tema, internette yayınlama.

## 9. Kabul ölçütleri

Mevcut testler ve yeni backend testleri geçer; TypeScript derlemesi hatasız; E2E testleri geçer; README'de çalıştırma adımları vardır.
