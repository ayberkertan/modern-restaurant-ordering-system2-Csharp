namespace RestoranSiparissistemi.Entity
{
    public class Kategori
    {
        public int KategoriId { get; set; }
        public string KategoriAdi { get; set; }
        public string Ikon { get; set; }

        public Kategori() { }

        public Kategori(int kategoriId, string kategoriAdi, string ikon = "🍽️")
        {
            KategoriId = kategoriId;
            KategoriAdi = kategoriAdi;
            Ikon = ikon;
        }

        public override string ToString() => KategoriAdi;
    }
}
