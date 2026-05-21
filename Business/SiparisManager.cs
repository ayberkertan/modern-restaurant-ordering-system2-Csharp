using System;
using System.Collections.Generic;
using System.Linq;
using RestoranSiparissistemi.Data;
using RestoranSiparissistemi.Entity;

namespace RestoranSiparissistemi.Business
{
    /// <summary>
    /// İş katmanı: Validasyon ve iş kuralları burada uygulanır.
    /// UI katmanı doğrudan DbHelper'a değil, buraya erişir.
    /// </summary>
    public class SiparisManager
    {
        private static readonly HashSet<string> GecerliSiparisDurumlari = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "Bekliyor", "Hazırlanıyor", "Teslim Edildi", "İptal"
        };

        private readonly DbHelper _db;

        public SiparisManager()
        {
            _db = new DbHelper();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BAŞLATMA
        // ═══════════════════════════════════════════════════════════════════════

        public void SistemiBaslat()
        {
            _db.VeritabaniOlustur();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // KATEGORİ
        // ═══════════════════════════════════════════════════════════════════════

        public List<Kategori> KategorileriGetir() => _db.KategorileriGetir();

        // ═══════════════════════════════════════════════════════════════════════
        // ÜRÜN
        // ═══════════════════════════════════════════════════════════════════════

        public List<Urun> UrunleriGetir(int? kategoriId = null) => _db.UrunleriGetir(kategoriId);

        public (bool Basarili, string Mesaj) UrunEkle(int kategoriId, string urunAdi, decimal fiyat)
        {
            if (string.IsNullOrWhiteSpace(urunAdi))
                return (false, "Ürün adı boş olamaz.");

            if (urunAdi.Trim().Length < 2)
                return (false, "Ürün adı en az 2 karakter olmalıdır.");

            if (fiyat <= 0)
                return (false, "Fiyat 0'dan büyük olmalıdır.");

            if (fiyat > 99999)
                return (false, "Fiyat çok yüksek girilmiş. Lütfen kontrol edin.");

            var urun = new Urun { KategoriId = kategoriId, UrunAdi = urunAdi.Trim(), Fiyat = fiyat };
            _db.UrunEkle(urun);
            return (true, "Ürün başarıyla eklendi.");
        }

        public (bool Basarili, string Mesaj) UrunGuncelle(int urunId, int kategoriId, string urunAdi, decimal fiyat)
        {
            if (urunId <= 0) return (false, "Geçersiz ürün.");
            if (string.IsNullOrWhiteSpace(urunAdi)) return (false, "Ürün adı boş olamaz.");
            if (fiyat <= 0) return (false, "Fiyat 0'dan büyük olmalıdır.");

            var urun = new Urun { UrunId = urunId, KategoriId = kategoriId, UrunAdi = urunAdi.Trim(), Fiyat = fiyat };
            _db.UrunGuncelle(urun);
            return (true, "Ürün güncellendi.");
        }

        public void UrunSil(int urunId) => _db.UrunSil(urunId);

        // ═══════════════════════════════════════════════════════════════════════
        // SİPARİŞ
        // ═══════════════════════════════════════════════════════════════════════

        public List<Siparis> MasaSiparisleriGetir(int masaNo) => _db.MasaSiparisleriGetir(masaNo);

        public (bool Basarili, string Mesaj) SiparisEkle(int masaNo, Urun urun, int adet)
        {
            if (masaNo <= 0)
                return (false, "Geçerli bir masa seçilmedi.");

            if (urun == null)
                return (false, "Lütfen bir ürün seçin.");

            if (adet <= 0)
                return (false, "Adet en az 1 olmalıdır.");

            if (adet > 99)
                return (false, "Tek seferde en fazla 99 adet ekleyebilirsiniz.");

            var siparis = new Siparis(masaNo, urun.UrunId, adet, urun.Fiyat);
            _db.SiparisEkle(siparis);

            return (true, $"{adet}x {urun.UrunAdi} sipariş alındı.");
        }

        public void SiparisSil(int siparisId) => _db.SiparisSil(siparisId);

        public (bool Basarili, string Mesaj) SiparisDurumGuncelle(int siparisId, string durum)
        {
            if (siparisId <= 0)
                return (false, "Geçersiz sipariş.");

            if (string.IsNullOrWhiteSpace(durum))
                return (false, "Durum seçilmedi.");

            var normalized = GecerliSiparisDurumlari.FirstOrDefault(d =>
                string.Equals(d, durum.Trim(), StringComparison.OrdinalIgnoreCase));

            if (normalized == null)
                return (false, "Geçersiz sipariş durumu.");

            _db.SiparisDurumGuncelle(siparisId, normalized);
            return (true, "Durum güncellendi.");
        }

        public (bool Basarili, string Mesaj) MasaOde(int masaNo)
        {
            if (masaNo <= 0) return (false, "Geçersiz masa.");

            decimal toplam = _db.MasaToplami(masaNo);
            if (toplam == 0) return (false, "Bu masada ödenecek sipariş bulunmuyor.");

            _db.MasaOde(masaNo);
            return (true, $"Masa {masaNo} hesabı kapatıldı. Toplam: {toplam:C2}");
        }

        public decimal MasaToplami(int masaNo) => _db.MasaToplami(masaNo);

        public Dictionary<int, bool> TumMasaDurumlari(int toplamMasa)
            => _db.TumMasaDurumlari(toplamMasa);
    }
}
