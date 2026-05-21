using System;

namespace RestoranSiparissistemi.Entity
{
    public class Siparis
    {
        public int SiparisId { get; set; }
        public int MasaNo { get; set; }
        public int UrunId { get; set; }
        public string UrunAdi { get; set; }   // JOIN için
        public int Adet { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal Toplam => Adet * BirimFiyat;
        public DateTime SiparisTarihi { get; set; }
        public string Durum { get; set; } // "Bekliyor", "Hazırlanıyor", "Teslim Edildi", "İptal"

        public Siparis()
        {
            SiparisTarihi = DateTime.Now;
            Durum = "Bekliyor";
        }

        public Siparis(int masaNo, int urunId, int adet, decimal birimFiyat)
        {
            MasaNo = masaNo;
            UrunId = urunId;
            Adet = adet;
            BirimFiyat = birimFiyat;
            SiparisTarihi = DateTime.Now;
            Durum = "Bekliyor";
        }
    }
}
