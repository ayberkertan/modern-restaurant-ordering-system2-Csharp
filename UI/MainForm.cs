using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RestoranSiparissistemi.Business;
using RestoranSiparissistemi.Entity;

namespace RestoranSiparissistemi.UI
{
    public partial class MainForm : Form
    {
        // ─── Renkler (Dark restaurant theme) ────────────────────────────────
        private static readonly Color C_BG         = Color.FromArgb(15,  15,  20);
        private static readonly Color C_PANEL      = Color.FromArgb(22,  22,  30);
        private static readonly Color C_CARD       = Color.FromArgb(30,  30,  42);
        private static readonly Color C_ACCENT     = Color.FromArgb(212, 160, 23);   // altın
        private static readonly Color C_ACCENT2    = Color.FromArgb(230, 90,  55);   // turuncu-kırmızı
        private static readonly Color C_GREEN      = Color.FromArgb(72,  199, 142);
        private static readonly Color C_TEXT       = Color.FromArgb(240, 240, 245);
        private static readonly Color C_TEXT_DIM   = Color.FromArgb(130, 130, 150);
        private static readonly Color C_MASA_BOSTA = Color.FromArgb(35,  35,  50);
        private static readonly Color C_MASA_DOLU  = Color.FromArgb(50,  30,  15);
        private static readonly Color C_BORDER     = Color.FromArgb(50,  50,  70);

        // ─── State ───────────────────────────────────────────────────────────
        private readonly SiparisManager _manager = new SiparisManager();
        private const int TOPLAM_MASA = 16;
        private int _seciliMasa = -1;
        private Urun _seciliUrun;
        private List<Urun> _tumUrunler;
        private List<Kategori> _kategoriler;
        private List<Button> _masaButonlari = new List<Button>();
        private int _aktifKategori = 0; // 0 = hepsi

        // ─── Kontroller ──────────────────────────────────────────────────────
        private Panel pnlLeft, pnlCenter, pnlRight;
        private Panel pnlHeader;
        private FlowLayoutPanel flpMasalar;
        private FlowLayoutPanel flpKategoriler;
        private FlowLayoutPanel flpUrunler;
        private DataGridView dgvSiparisler;
        private Label lblMasaBaslik, lblToplamFiyat, lblSeciliUrun;
        private NumericUpDown nudAdet;
        private Button btnSiparisEkle, btnSiparisSil, btnMasaOde, btnDurumGuncelle;
        private ComboBox cmbDurum;
        private TextBox txtUrunAra;
        private System.Windows.Forms.Timer tmrYenile;

        // ────────────────────────────────────────────────────────────────────
        public MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = C_BG;
            Font = new Font("Segoe UI", 9.5f);

            try
            {
                _manager.SistemiBaslat();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Veritabanı başlatılamadı.\n\n" +
                    "SQL Server LocalDB kurulu olduğundan ve App.config içindeki bağlantı ayarlarının doğru olduğundan emin olun.",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            ArayuzOlustur();
            VeriYukle();
            MasaGuncelle();
        }

        // ════════════════════════════════════════════════════════════════════
        // ARAYÜZ OLUŞTURMA
        // ════════════════════════════════════════════════════════════════════

        private void ArayuzOlustur()
        {
            // Header
            pnlHeader = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 60,
                BackColor = C_PANEL,
                Padding = new Padding(20, 0, 20, 0)
            };

            var lblLogo = new Label
            {
                Text      = "🍴  RESTORAN SİPARİŞ SİSTEMİ",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize  = false,
                Dock      = DockStyle.Left,
                Width     = 400,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSaat = new Label
            {
                Name      = "lblSaat",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = C_TEXT_DIM,
                AutoSize  = false,
                Dock      = DockStyle.Right,
                Width     = 200,
                TextAlign = ContentAlignment.MiddleRight
            };

            var headerSep = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 2,
                BackColor = C_ACCENT
            };

            pnlHeader.Controls.Add(lblLogo);
            pnlHeader.Controls.Add(lblSaat);
            pnlHeader.Controls.Add(headerSep);

            // Ana container
            var pnlMain = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 1,
                BackColor   = C_BG,
                Padding     = new Padding(12)
            };
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));   // Sol: Masalar
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));   // Orta: Menü
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));   // Sağ: Adisyon

            // ── SOL PANEL: Masalar ───────────────────────────────────────────
            pnlLeft = KartPanel(Padding(8));
            pnlLeft.Dock = DockStyle.Fill;

            var lblMasalarBaslik = SectionLabel("🪑  MASALAR");
            lblMasalarBaslik.Dock = DockStyle.Top;

            flpMasalar = new FlowLayoutPanel
            {
                Dock        = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoScroll    = true,
                Padding       = new Padding(4)
            };

            var legend = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.Transparent };
            var lblBosta = MiniLegend("⬜ Boş", C_TEXT_DIM);
            lblBosta.Dock = DockStyle.Left;
            var lblDolu = MiniLegend("🟧 Dolu", C_ACCENT2);
            lblDolu.Dock = DockStyle.Right;
            legend.Controls.AddRange(new Control[] { lblBosta, lblDolu });

            pnlLeft.Controls.Add(flpMasalar);
            pnlLeft.Controls.Add(legend);
            pnlLeft.Controls.Add(lblMasalarBaslik);

            // ── ORTA PANEL: Menü ─────────────────────────────────────────────
            pnlCenter = KartPanel(Padding(8));
            pnlCenter.Dock = DockStyle.Fill;

            var lblMenuBaslik = SectionLabel("📋  MENÜ");
            lblMenuBaslik.Dock = DockStyle.Top;

            // Arama
            var pnlAra = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 4) };
            txtUrunAra = new TextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = C_CARD,
                ForeColor   = C_TEXT,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 10f),
                //PlaceholderText = "🔍 Ürün ara..."
            };
            txtUrunAra.TextChanged += (s, e) => UrunleriFiltrele();
            pnlAra.Controls.Add(txtUrunAra);

            // Kategori sekmeler
            flpKategoriler = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 4, 0, 4),
                AutoScroll    = false
            };

            // Ürün listesi
            flpUrunler = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = Color.Transparent
            };

            pnlCenter.Controls.Add(flpUrunler);
            pnlCenter.Controls.Add(flpKategoriler);
            pnlCenter.Controls.Add(pnlAra);
            pnlCenter.Controls.Add(lblMenuBaslik);

            // ── SAĞ PANEL: Adisyon ───────────────────────────────────────────
            pnlRight = KartPanel(Padding(8));
            pnlRight.Dock = DockStyle.Fill;

            lblMasaBaslik = new Label
            {
                Text      = "Masa Seçilmedi",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                Dock      = DockStyle.Top,
                Height    = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(4, 0, 0, 0)
            };

            // Ürün ekleme alanı
            var pnlEkle = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = C_CARD, Padding = new Padding(8) };
            pnlEkle.Paint += (s, e) => RoundedBorder(e.Graphics, pnlEkle, C_BORDER, 6);

            lblSeciliUrun = new Label
            {
                Text      = "← Menüden ürün seçin",
                ForeColor = C_TEXT_DIM,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                Dock      = DockStyle.Top,
                Height    = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlAdetSatir = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.Transparent };
            var lblAdet = new Label { Text = "Adet:", ForeColor = C_TEXT, Width = 50, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft };
            nudAdet = new NumericUpDown
            {
                Minimum   = 1,
                Maximum   = 99,
                Value     = 1,
                Dock      = DockStyle.Left,
                Width     = 70,
                BackColor = C_BG,
                ForeColor = C_TEXT,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
            btnSiparisEkle = AksiyonButon("➕  SİPARİŞ EKLE", C_GREEN);
            btnSiparisEkle.Dock    = DockStyle.Fill;
            btnSiparisEkle.Enabled = false;
            btnSiparisEkle.Click  += BtnSiparisEkle_Click;
            pnlAdetSatir.Controls.AddRange(new Control[] { btnSiparisEkle, nudAdet, lblAdet });

            pnlEkle.Controls.Add(pnlAdetSatir);
            pnlEkle.Controls.Add(lblSeciliUrun);

            // DataGridView
            dgvSiparisler = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                BackgroundColor       = C_PANEL,
                GridColor             = C_BORDER,
                BorderStyle           = BorderStyle.None,
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                ReadOnly              = true,
                Font                  = new Font("Segoe UI", 9f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight   = 36,
                RowTemplate           = { Height = 32 }
            };
            dgvSiparisler.DefaultCellStyle.BackColor       = C_PANEL;
            dgvSiparisler.DefaultCellStyle.ForeColor       = C_TEXT;
            dgvSiparisler.DefaultCellStyle.SelectionBackColor = Color.FromArgb(212, 160, 23, 80);
            dgvSiparisler.DefaultCellStyle.SelectionForeColor = C_TEXT;
            dgvSiparisler.ColumnHeadersDefaultCellStyle.BackColor = C_CARD;
            dgvSiparisler.ColumnHeadersDefaultCellStyle.ForeColor = C_ACCENT;
            dgvSiparisler.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvSiparisler.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(28, 28, 40);
            dgvSiparisler.EnableHeadersVisualStyles = false;
            dgvSiparisler.SelectionChanged += DgvSiparisler_SelectionChanged;

            // Sütunlar
            dgvSiparisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "SiparisId", Visible = false });
            dgvSiparisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "UrunAdi",   HeaderText = "Ürün",   FillWeight = 40 });
            dgvSiparisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Adet",      HeaderText = "Adet",   FillWeight = 12 });
            dgvSiparisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "BirimFiyat",HeaderText = "Birim",  FillWeight = 20 });
            dgvSiparisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Toplam",    HeaderText = "Toplam", FillWeight = 20 });
            dgvSiparisler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Durum",     HeaderText = "Durum",  FillWeight = 22 });

            // Durum güncelleme satırı
            var pnlDurum = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.Transparent };
            cmbDurum = new ComboBox
            {
                Dock        = DockStyle.Left,
                Width       = 140,
                BackColor   = C_CARD,
                ForeColor   = C_TEXT,
                FlatStyle   = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbDurum.Items.AddRange(new[] { "Bekliyor", "Hazırlanıyor", "Teslim Edildi", "İptal" });
            cmbDurum.SelectedIndex = 0;
            btnDurumGuncelle = KucukButon("🔄 Güncelle", C_ACCENT);
            btnDurumGuncelle.Dock   = DockStyle.Left;
            btnDurumGuncelle.Click += BtnDurumGuncelle_Click;
            btnSiparisSil = KucukButon("🗑 Sil", C_ACCENT2);
            btnSiparisSil.Dock   = DockStyle.Right;
            btnSiparisSil.Click += BtnSiparisSil_Click;
            pnlDurum.Controls.AddRange(new Control[] { btnSiparisSil, btnDurumGuncelle, cmbDurum });

            // Toplam
            var pnlToplam = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = C_CARD };
            lblToplamFiyat = new Label
            {
                Text      = "TOPLAM:  ₺0,00",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = C_GREEN,
                Dock      = DockStyle.Left,
                Width     = 220,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            btnMasaOde = AksiyonButon("💳  HESAP KES", C_ACCENT2);
            btnMasaOde.Dock   = DockStyle.Right;
            btnMasaOde.Width  = 130;
            btnMasaOde.Click += BtnMasaOde_Click;
            pnlToplam.Controls.AddRange(new Control[] { lblToplamFiyat, btnMasaOde });

            pnlRight.Controls.Add(dgvSiparisler);
            pnlRight.Controls.Add(pnlDurum);
            pnlRight.Controls.Add(pnlToplam);
            pnlRight.Controls.Add(pnlEkle);
            pnlRight.Controls.Add(lblMasaBaslik);

            // Yerleştir
            pnlMain.Controls.Add(pnlLeft,   0, 0);
            pnlMain.Controls.Add(pnlCenter, 1, 0);
            pnlMain.Controls.Add(pnlRight,  2, 0);

            Controls.Add(pnlMain);
            Controls.Add(pnlHeader);

            // Saat timer
            tmrYenile = new System.Windows.Forms.Timer { Interval = 1000 };
            tmrYenile.Tick += (s, e) =>
            {
                var lbl = pnlHeader.Controls["lblSaat"] as Label;
                if (lbl != null) lbl.Text = DateTime.Now.ToString("HH:mm:ss  |  dd.MM.yyyy");
            };
            tmrYenile.Start();
        }

        // ════════════════════════════════════════════════════════════════════
        // VERİ YÜKLEME
        // ════════════════════════════════════════════════════════════════════

        private void VeriYukle()
        {
            _kategoriler = _manager.KategorileriGetir();
            _tumUrunler  = _manager.UrunleriGetir();

            KategoriButonarinıOlustur();
            UrunleriGoster(_tumUrunler);
        }

        private void KategoriButonarinıOlustur()
        {
            flpKategoriler.Controls.Clear();
            var btnHepsi = KategoriButon("🍽️ Hepsi", 0);
            btnHepsi.BackColor = C_ACCENT;
            btnHepsi.ForeColor = C_BG;
            flpKategoriler.Controls.Add(btnHepsi);

            foreach (var k in _kategoriler)
                flpKategoriler.Controls.Add(KategoriButon($"{k.Ikon} {k.KategoriAdi}", k.KategoriId));
        }

        private Button KategoriButon(string metin, int katId)
        {
            var btn = new Button
            {
                Text      = metin,
                Tag       = katId,
                AutoSize  = false,
                Width     = 120,
                Height    = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = C_CARD,
                ForeColor = C_TEXT,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(2)
            };
            btn.FlatAppearance.BorderColor = C_BORDER;
            btn.Click += (s, e) =>
            {
                _aktifKategori = katId;
                foreach (Button b in flpKategoriler.Controls.OfType<Button>())
                {
                    b.BackColor = C_CARD; b.ForeColor = C_TEXT;
                }
                btn.BackColor = C_ACCENT; btn.ForeColor = C_BG;
                UrunleriFiltrele();
            };
            return btn;
        }

        private void UrunleriFiltrele()
        {
            var aramaMetni = txtUrunAra.Text.Trim().ToLower();
            var filtreli = _tumUrunler
                .Where(u => (_aktifKategori == 0 || u.KategoriId == _aktifKategori)
                         && (string.IsNullOrEmpty(aramaMetni) || u.UrunAdi.ToLower().Contains(aramaMetni)))
                .ToList();
            UrunleriGoster(filtreli);
        }

        private void UrunleriGoster(List<Urun> urunler)
        {
            flpUrunler.Controls.Clear();
            foreach (var u in urunler)
                flpUrunler.Controls.Add(UrunKart(u));
        }

        private Panel UrunKart(Urun urun)
        {
            var pnl = new Panel
            {
                Width     = flpUrunler.ClientSize.Width - 20,
                Height    = 52,
                BackColor = C_CARD,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 0, 4),
                Tag       = urun
            };

            var lblAd = new Label
            {
                Text      = urun.UrunAdi,
                ForeColor = C_TEXT,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock      = DockStyle.Left,
                Width     = 180,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };

            var lblFiyat = new Label
            {
                Text      = $"₺{urun.Fiyat:N2}",
                ForeColor = C_ACCENT,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Dock      = DockStyle.Right,
                Width     = 90,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblKat = new Label
            {
                Text      = urun.KategoriAdi,
                ForeColor = C_TEXT_DIM,
                Font      = new Font("Segoe UI", 8f),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding   = new Padding(10, 0, 0, 6)
            };

            pnl.Controls.Add(lblKat);
            pnl.Controls.Add(lblFiyat);
            pnl.Controls.Add(lblAd);

            // Hover & Click
            Action<Color> vurgula = c => { pnl.BackColor = c; };
            pnl.MouseEnter += (s, e) => vurgula(Color.FromArgb(45, 45, 60));
            pnl.MouseLeave += (s, e) => vurgula(_seciliUrun?.UrunId == urun.UrunId ? Color.FromArgb(40, 35, 10) : C_CARD);
            pnl.Click      += (s, e) => UrunSec(urun, pnl);
            foreach (Control c in pnl.Controls)
            {
                c.MouseEnter += (s, e) => vurgula(Color.FromArgb(45, 45, 60));
                c.MouseLeave += (s, e) => vurgula(_seciliUrun?.UrunId == urun.UrunId ? Color.FromArgb(40, 35, 10) : C_CARD);
                c.Click      += (s, e) => UrunSec(urun, pnl);
            }
            return pnl;
        }

        private void UrunSec(Urun urun, Panel kaynakPanel)
        {
            _seciliUrun = urun;
            lblSeciliUrun.Text      = $"✅  {urun.UrunAdi}  —  ₺{urun.Fiyat:N2}";
            lblSeciliUrun.ForeColor = C_GREEN;
            btnSiparisEkle.Enabled  = _seciliMasa > 0;

            // Tüm kartları sıfırla, seçiliyi vurgula
            foreach (Panel p in flpUrunler.Controls.OfType<Panel>())
                p.BackColor = p == kaynakPanel ? Color.FromArgb(40, 35, 10) : C_CARD;
        }

        // ════════════════════════════════════════════════════════════════════
        // MASA PANELI
        // ════════════════════════════════════════════════════════════════════

        private void MasaGuncelle()
        {
            flpMasalar.Controls.Clear();
            _masaButonlari.Clear();

            var durumlar = _manager.TumMasaDurumlari(TOPLAM_MASA);

            for (int i = 1; i <= TOPLAM_MASA; i++)
            {
                int masaNo = i;
                bool dolu  = durumlar[masaNo];

                var btn = new Button
                {
                    Width     = 96,
                    Height    = 80,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = dolu ? C_MASA_DOLU : C_MASA_BOSTA,
                    ForeColor = dolu ? C_ACCENT2   : C_TEXT_DIM,
                    Cursor    = Cursors.Hand,
                    Margin    = new Padding(5),
                    Tag       = masaNo,
                    Text      = $"🪑\r\nMASA {masaNo}"
                };
                btn.FlatAppearance.BorderColor = dolu ? C_ACCENT2 : C_BORDER;
                btn.FlatAppearance.BorderSize  = 1;
                btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                if (masaNo == _seciliMasa)
                {
                    btn.BackColor = Color.FromArgb(40, 30, 5);
                    btn.ForeColor = C_ACCENT;
                    btn.FlatAppearance.BorderColor = C_ACCENT;
                    btn.FlatAppearance.BorderSize  = 2;
                }

                btn.Click += (s, e) => MasaSec(masaNo);
                btn.MouseEnter += (s, e) => { if (masaNo != _seciliMasa) btn.BackColor = Color.FromArgb(50, 50, 68); };
                btn.MouseLeave += (s, e) => { if (masaNo != _seciliMasa) btn.BackColor = dolu ? C_MASA_DOLU : C_MASA_BOSTA; };

                flpMasalar.Controls.Add(btn);
                _masaButonlari.Add(btn);
            }
        }

        private void MasaSec(int masaNo)
        {
            _seciliMasa = masaNo;
            lblMasaBaslik.Text = $"🪑  MASA {masaNo}  — ADİSYON";
            btnSiparisEkle.Enabled = _seciliUrun != null;
            AdisyonYukle();
            MasaGuncelle();
        }

        // ════════════════════════════════════════════════════════════════════
        // ADİSYON
        // ════════════════════════════════════════════════════════════════════

        private void AdisyonYukle()
        {
            dgvSiparisler.Rows.Clear();
            if (_seciliMasa <= 0) return;

            var siparisler = _manager.MasaSiparisleriGetir(_seciliMasa);
            foreach (var s in siparisler)
            {
                int idx = dgvSiparisler.Rows.Add(
                    s.SiparisId,
                    s.UrunAdi,
                    s.Adet,
                    $"₺{s.BirimFiyat:N2}",
                    $"₺{s.Toplam:N2}",
                    s.Durum);

                // Duruma göre satır rengi
                var row = dgvSiparisler.Rows[idx];
                row.DefaultCellStyle.ForeColor = s.Durum switch
                {
                    "Teslim Edildi" => C_GREEN,
                    "İptal"         => Color.FromArgb(150, 60, 60),
                    "Hazırlanıyor"  => C_ACCENT,
                    _               => C_TEXT
                };
            }

            decimal toplam = _manager.MasaToplami(_seciliMasa);
            lblToplamFiyat.Text = $"TOPLAM:  ₺{toplam:N2}";
        }

        // ════════════════════════════════════════════════════════════════════
        // OLAYLAR
        // ════════════════════════════════════════════════════════════════════

        private void BtnSiparisEkle_Click(object sender, EventArgs e)
        {
            if (_seciliMasa <= 0 || _seciliUrun == null) return;

            var (ok, msg) = _manager.SiparisEkle(_seciliMasa, _seciliUrun, (int)nudAdet.Value);
            if (ok)
            {
                AdisyonYukle();
                MasaGuncelle();
                nudAdet.Value = 1;
                Bildirim(msg, C_GREEN);
            }
            else Bildirim(msg, C_ACCENT2);
        }

        private void BtnSiparisSil_Click(object sender, EventArgs e)
        {
            if (dgvSiparisler.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvSiparisler.SelectedRows[0].Cells["SiparisId"].Value);
            if (MessageBox.Show("Bu sipariş silinsin mi?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _manager.SiparisSil(id);
                AdisyonYukle();
                MasaGuncelle();
            }
        }

        private void BtnDurumGuncelle_Click(object sender, EventArgs e)
        {
            if (dgvSiparisler.SelectedRows.Count == 0) return;
            int id    = Convert.ToInt32(dgvSiparisler.SelectedRows[0].Cells["SiparisId"].Value);
            string d  = cmbDurum.SelectedItem?.ToString() ?? "Bekliyor";
            var (ok, msg) = _manager.SiparisDurumGuncelle(id, d);
            if (ok)
            {
                AdisyonYukle();
                Bildirim(msg, C_ACCENT);
            }
            else
                Bildirim(msg, C_ACCENT2);
        }

        private void BtnMasaOde_Click(object sender, EventArgs e)
        {
            if (_seciliMasa <= 0) return;
            decimal toplam = _manager.MasaToplami(_seciliMasa);
            if (toplam == 0) { Bildirim("Bu masada açık hesap yok.", C_TEXT_DIM); return; }

            var onay = MessageBox.Show(
                $"Masa {_seciliMasa} hesabı kesilsin mi?\nToplam: ₺{toplam:N2}",
                "Hesap Kes", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (onay == DialogResult.Yes)
            {
                var (ok, msg) = _manager.MasaOde(_seciliMasa);
                Bildirim(msg, ok ? C_GREEN : C_ACCENT2);
                _seciliMasa = -1;
                lblMasaBaslik.Text = "Masa Seçilmedi";
                dgvSiparisler.Rows.Clear();
                lblToplamFiyat.Text = "TOPLAM:  ₺0,00";
                MasaGuncelle();
            }
        }

        private void DgvSiparisler_SelectionChanged(object sender, EventArgs e)
        {
            bool secili = dgvSiparisler.SelectedRows.Count > 0;
            btnSiparisSil.Enabled     = secili;
            btnDurumGuncelle.Enabled  = secili;
        }

        // ════════════════════════════════════════════════════════════════════
        // YARDIMCI
        // ════════════════════════════════════════════════════════════════════

        private void Bildirim(string mesaj, Color renk)
        {
            var lbl = new Label
            {
                Text      = mesaj,
                ForeColor = renk,
                BackColor = C_CARD,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(300, 36),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var parent = pnlRight;
            lbl.Location = new Point(parent.Width / 2 - 150, parent.Height - 80);
            parent.Controls.Add(lbl);
            lbl.BringToFront();

            var t = new System.Windows.Forms.Timer { Interval = 2500 };
            t.Tick += (s, e) => { t.Stop(); parent.Controls.Remove(lbl); lbl.Dispose(); };
            t.Start();
        }

        private Panel KartPanel(Padding padding)
        {
            return new Panel
            {
                BackColor = C_PANEL,
                Padding   = padding,
                Margin    = new Padding(4)
            };
        }

        private Label SectionLabel(string metin) => new Label
        {
            Text      = metin,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = C_ACCENT,
            Height    = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(4, 0, 0, 0)
        };

        private Label MiniLegend(string metin, Color renk) => new Label
        {
            Text      = metin,
            ForeColor = renk,
            Font      = new Font("Segoe UI", 8.5f),
            Width     = 80,
            TextAlign = ContentAlignment.MiddleCenter
        };

        private Button AksiyonButon(string metin, Color renk)
        {
            var btn = new Button
            {
                Text      = metin,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = renk,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand,
                Height    = 36
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button KucukButon(string metin, Color renk)
        {
            var btn = new Button
            {
                Text      = metin,
                Width     = 120,
                Height    = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = renk,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Margin    = new Padding(2)
            };
            btn.FlatAppearance.BorderSize  = 0;
            btn.Enabled = false;
            return btn;
        }

        private static Padding Padding(int all) => new Padding(all);

        private static void RoundedBorder(Graphics g, Control ctrl, Color renk, int radius)
        {
            using var path = new GraphicsPath();
            var r = ctrl.ClientRectangle;
            r.Inflate(-1, -1);
            path.AddArc(r.X, r.Y, radius, radius, 180, 90);
            path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
            path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            using var pen = new Pen(renk);
            g.DrawPath(pen, path);
        }
    }
}
