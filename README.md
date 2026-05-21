# Restoran Sipariş Sistemi

Windows masaüstü restoran sipariş ve adisyon uygulaması. Katmanlı mimari (Entity → Data → Business → UI), SQL Server LocalDB ve .NET Framework 4.7.2 kullanır.

---

## İçindekiler

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
- [GitHub’a yüklemeden önce](#githuba-yüklemeden-önce)
- [Lisans](#lisans)

---

## Genel bakış

Uygulama, restoran ortamında masa bazlı sipariş yönetimi sağlar: 16 masa, kategorili menü, adisyon görüntüleme, sipariş ekleme/silme, durum güncelleme ve hesap kesme. Veriler SQL Server LocalDB üzerinde kalıcıdır; ilk çalıştırmada tablolar ve örnek menü otomatik oluşturulur.

Bu proje **tek makinede çalışan** bir WinForms uygulamasıdır; web API, kullanıcı girişi veya ağ üzerinden erişim yoktur.

---

## Mimari

### Katman diyagramı

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
│  Veritabanı: RestoranSiparisVize (App.config)                  │
└─────────────────────────────────────────────────────────────────┘

        Entity (POCO) ── Kategori, Urun, Siparis
        Tüm katmanlar tarafından paylaşılır; dış bağımlılık yok
```

### Katman sorumlulukları

| Katman | Dosya(lar) | Sorumluluk |
|--------|------------|------------|
| **Entity** | `Entity/*.cs` | Saf veri modelleri (POCO). İş mantığı veya veritabanı kodu yok. |
| **Data** | `Data/DbHelper.cs` | Bağlantı, tablo oluşturma, örnek veri, CRUD ve sorgular. Parametreli SQL. |
| **Business** | `Business/SiparisManager.cs` | Validasyon (adet, fiyat, masa seçimi vb.), UI ile Data arasında köprü. |
| **UI** | `UI/MainForm.cs`, `Program.cs` | Arayüz, olaylar, görsel tema; doğrudan `DbHelper` çağrılmaz. |

### Bağımlılık yönü

```
UI → Business → Data → SQL
         ↘ Entity ↗
```

Entity katmanı diğer katmanlara bağımlı değildir. UI yalnızca `SiparisManager` üzerinden veriye erişir (katmanlı mimari kuralı).

---

## Proje yapısı

```
RestoranSiparissistemi/
├── RestoranSiparissistemi.sln      # Visual Studio çözüm dosyası
├── RestoranSiparissistemi.csproj   # Proje tanımı (.NET Framework 4.7.2)
├── App.config                      # Connection string
├── Program.cs                      # Uygulama giriş noktası
│
├── Entity/
│   ├── Kategori.cs
│   ├── Urun.cs
│   └── Siparis.cs
│
├── Data/
│   └── DbHelper.cs
│
├── Business/
│   └── SiparisManager.cs
│
└── UI/
    ├── MainForm.cs
    └── MainForm.Designer.cs
```

**Derleme çıktıları** (`bin/`, `obj/`), **IDE ayarları** (`.vs/`, `*.csproj.user`) repoya eklenmemelidir — bkz. [GitHub’a yüklemeden önce](#githuba-yüklemeden-önce).

---

## Veri modeli

### Tablolar

| Tablo | Açıklama |
|-------|----------|
| `Kategoriler` | Menü kategorileri (`KategoriAdi`, `Ikon`) |
| `Urunler` | Ürünler; `KategoriId` FK; `AktifMi` ile soft delete |
| `Siparisler` | Masa siparişleri; fiyat sipariş anında `BirimFiyat` olarak saklanır |

### Sipariş durumları (uygulama seviyesi)

`Bekliyor` → `Hazırlanıyor` → `Teslim Edildi` / `İptal` → ödeme sonrası `Ödendi`

Durumlar `NVARCHAR` olarak tutulur; veritabanında enum kısıtı yoktur.

### Şema oluşturma

`DbHelper.VeritabaniOlustur()` ilk çalıştırmada tabloları oluşturur; `OrnekVeriEkle()` kategoriler ve ürünler boşsa örnek menüyü ekler. `SiparisManager.SistemiBaslat()` bu akışı tetikler (`MainForm` constructor).

---

## İş akışı

1. Uygulama açılır → veritabanı/tablo kontrolü → örnek veri (gerekirse).
2. Sol panel: 16 masa (boş/dolu/seçili görsel durum).
3. Orta panel: kategori sekmeleri + ürün arama + ürün listesi.
4. Sağ panel: seçili masanın adisyonu (DataGridView), adet, sipariş ekle, durum güncelle, sil, hesap kes.
5. `MasaOde`: masadaki tüm açık siparişlerin durumu `Ödendi` olur.

---

## Teknoloji yığını

| Bileşen | Sürüm / not |
|---------|-------------|
| .NET Framework | 4.7.2 |
| UI | Windows Forms |
| Veritabanı | SQL Server Express LocalDB (`MSSQLLocalDB`) |
| Veri erişimi | `System.Data.SqlClient` 4.9.1 |
| Yapılandırma | `System.Configuration.ConfigurationManager` 10.0.6 |

---

## Gereksinimler

- Windows 10/11
- [Visual Studio 2019 veya 2022](https://visualstudio.microsoft.com/) (.NET desktop development workload)
- SQL Server Express LocalDB (genelde Visual Studio ile gelir)

---

## Kurulum ve çalıştırma

```bash
# Repoyu klonladıktan sonra
cd RestoranSiparissistemi
```

1. `RestoranSiparissistemi.sln` dosyasını Visual Studio ile açın.
2. NuGet paketlerinin geri yüklenmesini bekleyin (veya Solution Explorer → Restore NuGet Packages).
3. **F5** ile derleyip çalıştırın.

İlk çalıştırmada LocalDB üzerinde `RestoranSiparisVize` veritabanı ve tablolar oluşturulur; örnek menü yüklenir.

### Sorun giderme

| Sorun | Olası çözüm |
|-------|-------------|
| LocalDB bağlantı hatası | SQL Server LocalDB kurulu mu kontrol edin; `sqllocaldb info MSSQLLocalDB` |
| Veritabanı başlatılamadı | `App.config` içindeki connection string ve instance adını doğrulayın |
| Tablolar boş / hata | Uygulamayı yönetici olarak değil, normal kullanıcı ile çalıştırın; Integrated Security Windows hesabını kullanır |

---

## Yapılandırma

`App.config` içindeki bağlantı dizesi:

```xml
<connectionStrings>
  <add name="RestoranDB2"
       connectionString="Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=RestoranSiparisVize;Integrated Security=True;TrustServerCertificate=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

- **Kimlik doğrulama:** Windows Integrated Security (şifre repoda yok).
- **Catalog adı:** `RestoranSiparisVize` — değiştirmek için `App.config` ve mevcut LocalDB veritabanını uyumlu hale getirin.

---

## Özellikler

| Özellik | Açıklama |
|---------|----------|
| 16 masa | Buton tabanlı; dolu/boş/seçili renk durumu |
| Menü | Kategori filtreleri + anlık ürün arama |
| Adisyon | Masa seçilince sipariş listesi ve toplam |
| Sipariş | Ürün + adet (1–99) ile ekleme |
| Durum | Bekliyor / Hazırlanıyor / Teslim Edildi / İptal |
| Hesap kes | Masanın tüm açık siparişlerini ödendi olarak işaretle |
| Validasyon | Business katmanında (masa, ürün, adet, fiyat) |

### UI teması

Koyu restoran teması (altın/turuncu vurgular), duruma göre adisyon satır renklendirme, kısa süreli bildirim etiketleri (~2.5 sn).

---

## Lisans

MIT License — ayrıntılar için [LICENSE](LICENSE) dosyasına bakın.

---

## Ekran görüntüsü
<img width="1365" height="715" alt="Image" src="https://github.com/user-attachments/assets/5401047b-d4dd-4099-9d50-8293760e936e" />
