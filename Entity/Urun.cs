namespace RestoranSiparissistemi.Entity
{
    public class Urun
    {
        public int UrunId { get; set; }
        public int KategoriId { get; set; }
        public string UrunAdi { get; set; }
        public decimal Fiyat { get; set; }
        public bool AktifMi { get; set; }
        public string KategoriAdi { get; set; } // JOIN için

        public Urun() { AktifMi = true; }

        public Urun(int urunId, int kategoriId, string urunAdi, decimal fiyat, bool aktifMi = true)
        {
            UrunId = urunId;
            KategoriId = kategoriId;
            UrunAdi = urunAdi;
            Fiyat = fiyat;
            AktifMi = aktifMi;
        }

        public override string ToString() => $"{UrunAdi} - {Fiyat:C2}";
    }
}
