using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using RestoranSiparissistemi.Entity;

namespace RestoranSiparissistemi.Data
{
    public class DbHelper
    {
        private readonly string _connectionString;

        public DbHelper()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["RestoranDB2"].ConnectionString;
        }

        // ─── Bağlantı yönetimi ───────────────────────────────────────────────
        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        // ─── Veritabanı kurulumu ─────────────────────────────────────────────
        public void VeritabaniOlustur()
        {
            string[] sorgular = new[]
            {
                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Kategoriler' AND xtype='U')
                  CREATE TABLE Kategoriler (
                      KategoriId   INT IDENTITY(1,1) PRIMARY KEY,
                      KategoriAdi  NVARCHAR(100)     NOT NULL,
                      Ikon         NVARCHAR(10)      NOT NULL DEFAULT '🍽️'
                  )",

                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Urunler' AND xtype='U')
                  CREATE TABLE Urunler (
                      UrunId      INT IDENTITY(1,1) PRIMARY KEY,
                      KategoriId  INT           NOT NULL REFERENCES Kategoriler(KategoriId),
                      UrunAdi     NVARCHAR(150) NOT NULL,
                      Fiyat       DECIMAL(10,2) NOT NULL,
                      AktifMi     BIT           NOT NULL DEFAULT 1
                  )",

                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Siparisler' AND xtype='U')
                  CREATE TABLE Siparisler (
                      SiparisId      INT IDENTITY(1,1) PRIMARY KEY,
                      MasaNo         INT           NOT NULL,
                      UrunId         INT           NOT NULL REFERENCES Urunler(UrunId),
                      Adet           INT           NOT NULL,
                      BirimFiyat     DECIMAL(10,2) NOT NULL,
                      SiparisTarihi  DATETIME      NOT NULL DEFAULT GETDATE(),
                      Durum          NVARCHAR(50)  NOT NULL DEFAULT 'Bekliyor'
                  )"
            };

            using (var conn = GetConnection())
            {
                conn.Open();
                foreach (var sorgu in sorgular)
                {
                    using (var cmd = new SqlCommand(sorgu, conn))
                        cmd.ExecuteNonQuery();
                }
            }

            OrnekVeriEkle();
        }

        private void OrnekVeriEkle()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Kategori var mı?
                var kontrol = new SqlCommand("SELECT COUNT(*) FROM Kategoriler", conn);
                if ((int)kontrol.ExecuteScalar() > 0) return;

                // Kategoriler
                var kategoriler = new[]
                {
                    ("Başlangıçlar", "🥗"), ("Ana Yemekler", "🍖"), ("Pizzalar", "🍕"),
                    ("Burgerler", "🍔"), ("İçecekler", "🥤"), ("Tatlılar", "🍰")
                };

                foreach (var (adi, ikon) in kategoriler)
                {
                    var cmd = new SqlCommand(
                        "INSERT INTO Kategoriler (KategoriAdi, Ikon) VALUES (@a, @i)", conn);
                    cmd.Parameters.AddWithValue("@a", adi);
                    cmd.Parameters.AddWithValue("@i", ikon);
                    cmd.ExecuteNonQuery();
                }

                // Ürünler
                var urunler = new[]
                {
                    (1, "Mercimek Çorbası",      32.00m),
                    (1, "Domates Çorbası",       28.00m),
                    (1, "Sezar Salata",          55.00m),
                    (2, "Izgara Tavuk",          95.00m),
                    (2, "Kuzu Şiş",             130.00m),
                    (2, "Biftek (200g)",         180.00m),
                    (3, "Margarita Pizza",        85.00m),
                    (3, "Karışık Pizza",         100.00m),
                    (4, "Klasik Burger",          75.00m),
                    (4, "Smoky BBQ Burger",       90.00m),
                    (5, "Kola",                  25.00m),
                    (5, "Limonata",              30.00m),
                    (5, "Çay",                   15.00m),
                    (6, "Sütlaç",                35.00m),
                    (6, "Çikolatalı Brownie",    45.00m)
                };

                foreach (var (katId, adi, fiyat) in urunler)
                {
                    var cmd = new SqlCommand(
                        "INSERT INTO Urunler (KategoriId, UrunAdi, Fiyat) VALUES (@k, @a, @f)", conn);
                    cmd.Parameters.AddWithValue("@k", katId);
                    cmd.Parameters.AddWithValue("@a", adi);
                    cmd.Parameters.AddWithValue("@f", fiyat);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // KATEGORİ İŞLEMLERİ
        // ═══════════════════════════════════════════════════════════════════════

        public List<Kategori> KategorileriGetir()
        {
            var liste = new List<Kategori>();
            const string sql = "SELECT KategoriId, KategoriAdi, Ikon FROM Kategoriler ORDER BY KategoriId";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        liste.Add(new Kategori(
                            reader.GetInt32(0),
                            reader.GetString(1),
                            reader.GetString(2)));
            }
            return liste;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ÜRÜN İŞLEMLERİ
        // ═══════════════════════════════════════════════════════════════════════

        public List<Urun> UrunleriGetir(int? kategoriId = null)
        {
            var liste = new List<Urun>();
            string sql = @"SELECT u.UrunId, u.KategoriId, u.UrunAdi, u.Fiyat, u.AktifMi, k.KategoriAdi
                           FROM Urunler u
                           INNER JOIN Kategoriler k ON u.KategoriId = k.KategoriId
                           WHERE u.AktifMi = 1"
                         + (kategoriId.HasValue ? " AND u.KategoriId = @katId" : "")
                         + " ORDER BY u.UrunAdi";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (kategoriId.HasValue)
                    cmd.Parameters.AddWithValue("@katId", kategoriId.Value);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        var u = new Urun(
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetString(2),
                            reader.GetDecimal(3),
                            reader.GetBoolean(4));
                        u.KategoriAdi = reader.GetString(5);
                        liste.Add(u);
                    }
            }
            return liste;
        }

        public void UrunEkle(Urun urun)
        {
            const string sql = @"INSERT INTO Urunler (KategoriId, UrunAdi, Fiyat, AktifMi)
                                 VALUES (@k, @a, @f, 1)";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@k", urun.KategoriId);
                cmd.Parameters.AddWithValue("@a", urun.UrunAdi);
                cmd.Parameters.AddWithValue("@f", urun.Fiyat);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UrunGuncelle(Urun urun)
        {
            const string sql = @"UPDATE Urunler SET KategoriId=@k, UrunAdi=@a, Fiyat=@f
                                 WHERE UrunId=@id";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@k", urun.KategoriId);
                cmd.Parameters.AddWithValue("@a", urun.UrunAdi);
                cmd.Parameters.AddWithValue("@f", urun.Fiyat);
                cmd.Parameters.AddWithValue("@id", urun.UrunId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UrunSil(int urunId)
        {
            const string sql = "UPDATE Urunler SET AktifMi=0 WHERE UrunId=@id";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", urunId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SİPARİŞ İŞLEMLERİ
        // ═══════════════════════════════════════════════════════════════════════

        public List<Siparis> MasaSiparisleriGetir(int masaNo)
        {
            var liste = new List<Siparis>();
            const string sql = @"
                SELECT s.SiparisId, s.MasaNo, s.UrunId, u.UrunAdi,
                       s.Adet, s.BirimFiyat, s.SiparisTarihi, s.Durum
                FROM Siparisler s
                INNER JOIN Urunler u ON s.UrunId = u.UrunId
                WHERE s.MasaNo = @masa AND s.Durum <> 'Ödendi'
                ORDER BY s.SiparisTarihi";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@masa", masaNo);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        liste.Add(new Siparis
                        {
                            SiparisId     = reader.GetInt32(0),
                            MasaNo        = reader.GetInt32(1),
                            UrunId        = reader.GetInt32(2),
                            UrunAdi       = reader.GetString(3),
                            Adet          = reader.GetInt32(4),
                            BirimFiyat    = reader.GetDecimal(5),
                            SiparisTarihi = reader.GetDateTime(6),
                            Durum         = reader.GetString(7)
                        });
                    }
            }
            return liste;
        }

        public void SiparisEkle(Siparis siparis)
        {
            const string sql = @"INSERT INTO Siparisler (MasaNo, UrunId, Adet, BirimFiyat, SiparisTarihi, Durum)
                                 VALUES (@masa, @urun, @adet, @fiyat, @tarih, @durum)";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@masa",  siparis.MasaNo);
                cmd.Parameters.AddWithValue("@urun",  siparis.UrunId);
                cmd.Parameters.AddWithValue("@adet",  siparis.Adet);
                cmd.Parameters.AddWithValue("@fiyat", siparis.BirimFiyat);
                cmd.Parameters.AddWithValue("@tarih", siparis.SiparisTarihi);
                cmd.Parameters.AddWithValue("@durum", siparis.Durum);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void SiparisDurumGuncelle(int siparisId, string durum)
        {
            const string sql = "UPDATE Siparisler SET Durum=@durum WHERE SiparisId=@id";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@durum", durum);
                cmd.Parameters.AddWithValue("@id",    siparisId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void SiparisSil(int siparisId)
        {
            const string sql = "DELETE FROM Siparisler WHERE SiparisId=@id";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", siparisId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void MasaOde(int masaNo)
        {
            const string sql = "UPDATE Siparisler SET Durum='Ödendi' WHERE MasaNo=@masa AND Durum <> 'Ödendi'";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@masa", masaNo);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public decimal MasaToplami(int masaNo)
        {
            const string sql = @"SELECT ISNULL(SUM(Adet * BirimFiyat), 0)
                                 FROM Siparisler
                                 WHERE MasaNo=@masa AND Durum <> 'Ödendi'";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@masa", masaNo);
                conn.Open();
                return (decimal)cmd.ExecuteScalar();
            }
        }

        public bool MasaDolu(int masaNo)
        {
            const string sql = @"SELECT COUNT(*) FROM Siparisler
                                 WHERE MasaNo=@masa AND Durum <> 'Ödendi'";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@masa", masaNo);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        // Tüm masaların doluluk durumu
        public Dictionary<int, bool> TumMasaDurumlari(int toplamMasa)
        {
            var sonuc = new Dictionary<int, bool>();
            for (int i = 1; i <= toplamMasa; i++) sonuc[i] = false;

            const string sql = @"SELECT DISTINCT MasaNo FROM Siparisler WHERE Durum <> 'Ödendi'";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        int masa = r.GetInt32(0);
                        if (sonuc.ContainsKey(masa)) sonuc[masa] = true;
                    }
            }
            return sonuc;
        }
    }
}
