# Restoran Sipariş Sistemi / Restaurant Order System

**TR** — Windows masaüstü restoran sipariş ve adisyon uygulaması. Katmanlı mimari (Entity → Data → Business → UI), SQL Server LocalDB ve .NET Framework 4.7.2.

**EN** — Windows desktop restaurant ordering and bill management app. Layered architecture (Entity → Data → Business → UI), SQL Server LocalDB, and .NET Framework 4.7.2.

**Languages:** [Türkçe](#türkçe) · [English](#english)

---

<a id="türkçe"></a>

## Türkçe

### İçindekiler

- [Genel bakış](#genel-bakış)
- [Mimari](#mimari)
- [Proje yapısı](#proje-yapısı)
- [Veri modeli](#veri-modeli)
- [İş akışı](#iş-akışı)
- [Teknoloji yığını](#teknoloji-yığını)
- [Gereksinimler](#gereksinimler)
- [Kurulum ve çalıştırma](#kurulum-ve-çalıştırma)
- [Yapılandırma](#yapılandırma)
- [Özellikler](#özellikler)
- [Repoya dahil edilmemesi gerekenler](#repoya-dahil-edilmemesi-gerekenler)
- [Lisans](#lisans)

---

### Genel bakış

Uygulama, restoran ortamında masa bazlı sipariş yönetimi sağlar: 16 masa, kategorili menü, adisyon görüntüleme, sipariş ekleme/silme, durum güncelleme ve hesap kesme. Veriler SQL Server LocalDB üzerinde kalıcıdır; ilk çalıştırmada tablolar ve örnek menü otomatik oluşturulur.

Bu proje **tek makinede çalışan** bir WinForms uygulamasıdır; web API, kullanıcı girişi veya ağ üzerinden erişim yoktur.

---

### Mimari

#### Katman diyagramı

```
┌─────────────────────────────────────────────────────────────────┐
│                         UI (Windows Forms)                       │
│  MainForm.cs — masa, menü, adisyon, kullanıcı etkileşimi        │
└───────────────────────────────┬─────────────────────────────────┘
                                │ SiparisManager
┌───────────────────────────────▼─────────────────────────────────┐
│                      Business (İş kuralları)                     │
│  SiparisManager.cs — validasyon, sipariş/ürün orchestration     │
└───────────────────────────────┬─────────────────────────────────┘
                                │ DbHelper
┌───────────────────────────────▼─────────────────────────────────┐
│                      Data (Veri erişimi)                         │
│  DbHelper.cs — SQL sorguları, bağlantı, şema + seed              │
└───────────────────────────────┬─────────────────────────────────┘
                                │ ADO.NET (SqlClient)
┌───────────────────────────────▼─────────────────────────────────┐
│                   SQL Server LocalDB                             │
│  Veritabanı: RestoranSiparisVize (App.config)                    │
└─────────────────────────────────────────────────────────────────┘

        Entity (POCO) ── Kategori, Urun, Siparis
        Tüm katmanlar tarafından paylaşılır; dış bağımlılık yok
```

#### Katman sorumlulukları

| Katman | Dosya(lar) | Sorumluluk |
|--------|------------|------------|
| **Entity** | `Entity/*.cs` | Saf veri modelleri (POCO). İş mantığı veya veritabanı kodu yok. |
| **Data** | `Data/DbHelper.cs` | Bağlantı, tablo oluşturma, örnek veri, CRUD ve sorgular. Parametreli SQL. |
| **Business** | `Business/SiparisManager.cs` | Validasyon (adet, fiyat, masa seçimi vb.), UI ile Data arasında köprü. |
| **UI** | `UI/MainForm.cs`, `Program.cs` | Arayüz, olaylar, görsel tema; doğrudan `DbHelper` çağrılmaz. |

#### Bağımlılık yönü

```
UI → Business → Data → SQL
         ↘ Entity ↗
```

Entity katmanı diğer katmanlara bağımlı değildir. UI yalnızca `SiparisManager` üzerinden veriye erişir.

---

### Proje yapısı

```
├── RestoranSiparissistemi.sln
├── RestoranSiparissistemi.csproj
├── App.config
├── Program.cs
├── Entity/          (Kategori, Urun, Siparis)
├── Data/            (DbHelper.cs)
├── Business/        (SiparisManager.cs)
└── UI/              (MainForm.cs, MainForm.Designer.cs)
```

`bin/`, `obj/`, `.vs/`, `*.user` dosyaları repoya eklenmemelidir — `.gitignore` ile hariç tutulur.

---

### Veri modeli

| Tablo | Açıklama |
|-------|----------|
| `Kategoriler` | Menü kategorileri (`KategoriAdi`, `Ikon`) |
| `Urunler` | Ürünler; `KategoriId` FK; `AktifMi` ile soft delete |
| `Siparisler` | Masa siparişleri; fiyat sipariş anında `BirimFiyat` olarak saklanır |

**Sipariş durumları:** `Bekliyor` → `Hazırlanıyor` → `Teslim Edildi` / `İptal` → ödeme sonrası `Ödendi`

`DbHelper.VeritabaniOlustur()` tabloları oluşturur; `OrnekVeriEkle()` boş veritabanına örnek menü ekler.

---

### İş akışı

1. Uygulama açılır → veritabanı/tablo kontrolü → örnek veri (gerekirse).
2. Sol panel: 16 masa (boş/dolu/seçili).
3. Orta panel: kategori sekmeleri + ürün arama + ürün listesi.
4. Sağ panel: adisyon, sipariş ekle, durum güncelle, sil, hesap kes.
5. `MasaOde`: masadaki açık siparişler `Ödendi` olur.

---

### Teknoloji yığını

| Bileşen | Sürüm / not |
|---------|-------------|
| .NET Framework | 4.7.2 |
| UI | Windows Forms |
| Veritabanı | SQL Server Express LocalDB |
| Veri erişimi | `System.Data.SqlClient` 4.9.1 |
| Yapılandırma | `System.Configuration.ConfigurationManager` 10.0.6 |

---

### Gereksinimler

- Windows 10/11
- [Visual Studio 2019 veya 2022](https://visualstudio.microsoft.com/) (.NET desktop development)
- SQL Server Express LocalDB

---

### Kurulum ve çalıştırma

```bash
git clone https://github.com/ayberkertan/modern-restaurant-ordering-system2-Csharp.git
cd modern-restaurant-ordering-system2-Csharp
```

1. `RestoranSiparissistemi.sln` dosyasını Visual Studio ile açın.
2. NuGet paketlerinin geri yüklenmesini bekleyin.
3. **F5** ile çalıştırın.

İlk çalıştırmada `RestoranSiparisVize` veritabanı ve örnek menü oluşturulur.

| Sorun | Olası çözüm |
|-------|-------------|
| LocalDB bağlantı hatası | `sqllocaldb info MSSQLLocalDB` |
| Veritabanı başlatılamadı | `App.config` connection string kontrolü |
| Tablolar boş / hata | Normal kullanıcı ile çalıştırın (Integrated Security) |

---

### Yapılandırma

```xml
<connectionStrings>
  <add name="RestoranDB2"
       connectionString="Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=RestoranSiparisVize;Integrated Security=True;TrustServerCertificate=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

- **Kimlik doğrulama:** Windows Integrated Security (repoda şifre yok).
- **Catalog:** `RestoranSiparisVize`

---

### Özellikler

| Özellik | Açıklama |
|---------|----------|
| 16 masa | Dolu/boş/seçili görsel durum |
| Menü | Kategori filtreleri + anlık arama |
| Adisyon | Sipariş listesi ve toplam |
| Sipariş | Ürün + adet (1–99) |
| Durum | Bekliyor / Hazırlanıyor / Teslim Edildi / İptal |
| Hesap kes | Açık siparişleri ödendi işaretle |
| Validasyon | Business katmanında |

**UI:** Koyu restoran teması (altın/turuncu), duruma göre satır renklendirme, toast bildirimler (~2.5 sn).

---

### Repoya dahil edilmemesi gerekenler

`bin/`, `obj/`, `.vs/`, `*.user`, `*.mdf`, `*.ldf` — `.gitignore` ile hariç tutulur.

> Şema `DbHelper.VeritabaniOlustur()` ile oluşturulur; ayrı SQL script dosyası yoktur.

---

<a id="english"></a>

## English

### Table of contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Project structure](#project-structure)
- [Data model](#data-model)
- [Workflow](#workflow)
- [Tech stack](#tech-stack)
- [Requirements](#requirements)
- [Setup and run](#setup-and-run)
- [Configuration](#configuration)
- [Features](#features)
- [What not to commit](#what-not-to-commit)
- [License](#license)

---

### Overview

A desktop restaurant management app for **table-based ordering**: 16 tables, categorized menu, live bill (check) view, add/remove orders, status updates, and checkout. Data persists in **SQL Server LocalDB**; tables and a sample menu are created automatically on first run.

This is a **single-machine** WinForms app — no web API, no login, and no network access.

---

### Architecture

#### Layer diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         UI (Windows Forms)                       │
│  MainForm.cs — tables, menu, bill, user interaction              │
└───────────────────────────────┬─────────────────────────────────┘
                                │ SiparisManager
┌───────────────────────────────▼─────────────────────────────────┐
│                      Business (business rules)                   │
│  SiparisManager.cs — validation, order/product orchestration     │
└───────────────────────────────┬─────────────────────────────────┘
                                │ DbHelper
┌───────────────────────────────▼─────────────────────────────────┐
│                      Data (data access)                          │
│  DbHelper.cs — SQL, connection, schema + seed data               │
└───────────────────────────────┬─────────────────────────────────┘
                                │ ADO.NET (SqlClient)
┌───────────────────────────────▼─────────────────────────────────┐
│                   SQL Server LocalDB                             │
│  Database: RestoranSiparisVize (App.config)                      │
└─────────────────────────────────────────────────────────────────┘

        Entity (POCO) — Category, Product, Order
        Shared across layers; no external dependencies
```

#### Layer responsibilities

| Layer | Files | Responsibility |
|-------|-------|----------------|
| **Entity** | `Entity/*.cs` | Plain POCO models. No business or DB logic. |
| **Data** | `Data/DbHelper.cs` | Connection, schema creation, seed data, CRUD. Parameterized SQL. |
| **Business** | `Business/SiparisManager.cs` | Validation (quantity, price, table, status whitelist). Bridge between UI and Data. |
| **UI** | `UI/MainForm.cs`, `Program.cs` | UI, events, dark theme. Never calls `DbHelper` directly. |

#### Dependency direction

```
UI → Business → Data → SQL
         ↘ Entity ↗
```

The UI only accesses data through `SiparisManager` (layered architecture).

---

### Project structure

```
├── RestoranSiparissistemi.sln
├── RestoranSiparissistemi.csproj
├── App.config
├── Program.cs
├── Entity/          (Category, Product, Order models)
├── Data/            (DbHelper.cs)
├── Business/        (SiparisManager.cs)
└── UI/              (MainForm.cs, MainForm.Designer.cs)
```

Do not commit `bin/`, `obj/`, `.vs/`, or `*.user` — covered by `.gitignore`.

---

### Data model

| Table | Description |
|-------|-------------|
| `Kategoriler` | Menu categories (`KategoriAdi`, `Ikon`) |
| `Urunler` | Products; FK to category; soft delete via `AktifMi` |
| `Siparisler` | Table orders; `BirimFiyat` stores price at order time |

**Order statuses:** `Bekliyor` (Waiting) → `Hazırlanıyor` (Preparing) → `Teslim Edildi` (Delivered) / `İptal` (Cancelled) → `Ödendi` (Paid) after checkout.

`DbHelper.VeritabaniOlustur()` creates tables; `OrnekVeriEkle()` seeds sample categories and products when empty.

---

### Workflow

1. App starts → DB/schema check → seed data if needed.
2. Left panel: 16 tables (empty/occupied/selected states).
3. Center panel: category tabs + product search + product list.
4. Right panel: bill grid, add order, update status, delete, checkout.
5. `MasaOde`: marks all open orders on the table as paid.

---

### Tech stack

| Component | Version / note |
|-----------|----------------|
| .NET Framework | 4.7.2 |
| UI | Windows Forms |
| Database | SQL Server Express LocalDB |
| Data access | `System.Data.SqlClient` 4.9.1 |
| Configuration | `System.Configuration.ConfigurationManager` 10.0.6 |

---

### Requirements

- Windows 10/11
- [Visual Studio 2019 or 2022](https://visualstudio.microsoft.com/) (.NET desktop development workload)
- SQL Server Express LocalDB (usually installed with Visual Studio)

---

### Setup and run

```bash
git clone https://github.com/ayberkertan/modern-restaurant-ordering-system2-Csharp.git
cd modern-restaurant-ordering-system2-Csharp
```

1. Open `RestoranSiparissistemi.sln` in Visual Studio.
2. Wait for NuGet package restore.
3. Press **F5** to build and run.

On first run, the `RestoranSiparisVize` database and sample menu are created automatically.

| Issue | Possible fix |
|-------|----------------|
| LocalDB connection error | Run `sqllocaldb info MSSQLLocalDB` |
| Database failed to start | Check `App.config` connection string |
| Empty tables / errors | Run as normal user (Integrated Security uses Windows account) |

---

### Configuration

```xml
<connectionStrings>
  <add name="RestoranDB2"
       connectionString="Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=RestoranSiparisVize;Integrated Security=True;TrustServerCertificate=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

- **Authentication:** Windows Integrated Security (no password in repo).
- **Catalog name:** `RestoranSiparisVize`

---

### Features

| Feature | Description |
|---------|-------------|
| 16 tables | Button-based; empty/occupied/selected colors |
| Menu | Category filters + live product search |
| Bill | Order list and total for selected table |
| Orders | Product + quantity (1–99) |
| Status | Waiting / Preparing / Delivered / Cancelled |
| Checkout | Mark all open orders as paid |
| Validation | Business layer (table, product, quantity, price, status) |

**UI:** Dark restaurant theme (gold/orange accents), row colors by status, short toast notifications (~2.5 s).

---

### What not to commit

`bin/`, `obj/`, `.vs/`, `*.user`, `*.mdf`, `*.ldf` — excluded via `.gitignore`.

> Schema is created in code via `DbHelper.VeritabaniOlustur()`; there is no separate SQL script file.

---

<a id="license"></a>

## Lisans / License

MIT License — see [LICENSE](LICENSE).

---

## Ekran görüntüsü / Screenshot

<img width="1365" height="715" alt="Image" src="https://github.com/user-attachments/assets/4c2988c1-3d3a-4a9b-8272-a9b0efd8549e" />
