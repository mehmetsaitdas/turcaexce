using System.Diagnostics;
using System.Reflection;
using TurcaExce.Config;
using TurcaExce.Models;
using TurcaExce.Services;

namespace TurcaExce
{
    public partial class MainForm : Form
    {
        // Son dönüşümün satırları/özeti: hem gridPreview'un veri kaynağı hem de
        // Kalite Revize'nin üzerinde çalışıp txtTargetPath'e yeniden yazdığı liste.
        private List<ProductEntryRow>? _currentRows;
        private ManualOrderSummary? _currentSummary;

        public MainForm()
        {
            InitializeComponent();
            // Pencere ikonu = exe'ye gömülü uygulama ikonu (Resources\app.ico).
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            // Sürüm başlık çubuğunda görünür (kullanıcı destek isterken hangi
            // sürümü kullandığını söyleyebilsin). Kaynağı csproj'daki
            // AssemblyVersion; Application.ProductVersion kullanılmıyor çünkü o
            // AssemblyVersion'ı değil InformationalVersion'ı okur ve ayarlı
            // olmadığı için son haneyi ("1.0.0.3" -> "1.0.0") kaybediyordu.
            Text = $"{Text}   —   Sürüm {AppVersion}";

            lblSettingsFile.Text =
                $"Sürüm: {AppVersion}   |   Eşleşme tabloları: {ConversionSettings.FilePath}   |   Renkler: {ColorRegistry.FilePath}   |   EAN kayıtları: {EanRegistry.FilePath}";

            var settings = ConversionSettings.Load();

            // İlk açılışta koda gömülü statik renkler colors.txt'ye yazılır;
            // sonraki açılışlarda renkler bu dosyadan okunur.
            ColorRegistry.Load(settings.ColorMap);

            // Kayıtlı alıcı adresini geri yükle.
            txtEmail.Text = settings.Email.Recipient;
            UpdateMailStatusLabel(settings.Email);

            InitializeCompanyName();
        }

        /// <summary>csproj'daki AssemblyVersion, görüntülenmeye hazır hali (örn. "1.0.0.3").</summary>
        private static string AppVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "bilinmiyor";

        /// <summary>
        /// config.ini'de firma adı yoksa (ilk açılış) girilmesini zorunlu
        /// kılar; varsa butona yazar. Ser numaralarının firmalar arasında
        /// çakışmaması bu ada bağlıdır (bkz. CompanyConfig.SerialPrefix).
        /// </summary>
        private void InitializeCompanyName()
        {
            var company = CompanyConfig.Load();
            if (string.IsNullOrWhiteSpace(company.Name))
            {
                var name = PromptCompanyName("", allowCancel: false);
                company.Name = name ?? "";
                company.Save();
            }
            UpdateCompanyButton(company);
        }

        private void UpdateCompanyButton(CompanyConfig company) =>
            btnCompany.Text = $"Firma: {company.Name}";

        private void btnCompany_Click(object? sender, EventArgs e)
        {
            var company = CompanyConfig.Load();
            var name = PromptCompanyName(company.Name, allowCancel: true);
            if (name == null) return; // vazgeçildi

            company.Name = name;
            company.Save();
            UpdateCompanyButton(company);
        }

        private void btnSupport_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://turca.app/tr#contact") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Bağlantı açılamadı",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Firma adını sorar. allowCancel false ise (ilk açılış) yalnızca
        /// boş olmayan bir ad girilince kapanır, "İptal" gösterilmez.
        /// Vazgeçilirse (allowCancel true iken) null döner.
        /// </summary>
        private string? PromptCompanyName(string currentName, bool allowCancel)
        {
            using var dialog = new Form
            {
                Text = "Firma Adı",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(380, 130),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ControlBox = allowCancel,
            };

            var lbl = new Label
            {
                Text = "Firma adını yazın (Ser numaralarının önekinde kullanılır):",
                Location = new Point(12, 12),
                Size = new Size(356, 36),
            };
            var txt = new TextBox { Location = new Point(12, 54), Size = new Size(356, 23), Text = currentName };
            var btnOk = new Button
            {
                Text = "Kaydet",
                DialogResult = DialogResult.OK,
                Location = new Point(212, 90),
                Size = new Size(75, 28),
                Enabled = currentName.Trim().Length > 0,
            };
            txt.TextChanged += (_, _) => btnOk.Enabled = txt.Text.Trim().Length > 0;

            dialog.Controls.Add(lbl);
            dialog.Controls.Add(txt);
            dialog.Controls.Add(btnOk);
            dialog.AcceptButton = btnOk;

            if (allowCancel)
            {
                var btnCancel = new Button
                {
                    Text = "İptal",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(293, 90),
                    Size = new Size(75, 28),
                };
                dialog.Controls.Add(btnCancel);
                dialog.CancelButton = btnCancel;
            }
            else
            {
                btnOk.Location = new Point(293, 90);
            }

            return dialog.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
        }

        /// <summary>Alt satırdaki Outlook giriş durumunu günceller.</summary>
        private void UpdateMailStatusLabel(Config.EmailSettings email) =>
            lblMailStatus.Text = string.IsNullOrWhiteSpace(email.SignedInAccount)
                ? "(Outlook girişi yapılmadı)"
                : $"Outlook girişi: {email.SignedInAccount}";

        private void btnBrowseSource_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Üretim emri dosyasını seçin (123.xls)",
                Filter = "Excel dosyaları (*.xls;*.xlsx)|*.xls;*.xlsx|Tüm dosyalar (*.*)|*.*",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            txtSourcePath.Text = dialog.FileName;
            // Çıktı adı kalıbı: 123.xls -> 123_TRC.xlsx
            // Her yeni kaynak seçiminde güncellenir; eski ad kalırsa üzerine yazılıyordu.
            var folder = Path.GetDirectoryName(dialog.FileName) ?? "";
            txtTargetPath.Text = Path.Combine(folder,
                Path.GetFileNameWithoutExtension(dialog.FileName) + "_TRC.xlsx");
        }

        private void btnBrowseTarget_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Çıktı dosyasını kaydet",
                Filter = "Excel çalışma kitabı (*.xlsx)|*.xlsx",
                FileName = string.IsNullOrWhiteSpace(txtTargetPath.Text)
                    ? "Excel_Girisi.xlsx"
                    : Path.GetFileName(txtTargetPath.Text),
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                txtTargetPath.Text = dialog.FileName;
        }

        private void btnConvert_Click(object? sender, EventArgs e)
        {
            if (!File.Exists(txtSourcePath.Text))
            {
                MessageBox.Show(this, "Önce geçerli bir kaynak dosya seçin.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTargetPath.Text))
            {
                MessageBox.Show(this, "Çıktı dosyası yolunu belirtin.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Dönüştürülüyor...";

                var settings = ConversionSettings.Load();
                var eanRegistry = EanRegistry.Load(settings.StartingEan);
                var colorRegistry = ColorRegistry.Load(settings.ColorMap);
                var sizeRegistry = SizeRegistry.Load();
                var serialRegistry = SerialRegistry.Load();
                var sourceLines = ProductionOrderReader.Read(txtSourcePath.Text);
                var result = new ConversionService(settings, eanRegistry, colorRegistry, sizeRegistry, serialRegistry,
                        PromptColorName, PromptSizeName, PromptQualityName)
                    .Convert(sourceLines);

                ApplyConversionResult(result, eanRegistry, txtTargetPath.Text,
                    $"{sourceLines.Count} desen satırı okundu, {result.Rows.Count} ürün satırı yazıldı");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Hata oluştu.";
                MessageBox.Show(this, ex.Message, "Dönüşüm hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Üretim emri Excel'i olmayan müşteriler için: ekrandaki pivot
        /// tablodan (Desen/Renk/Ebat/Adet) elle üretim girişi alır. Kalite,
        /// bilinen renkler ve ebatlar ManualOrderForm içinde önerilir; yeni
        /// girilenler ilgili kayıt defterlerine eklenir.
        /// </summary>
        private void btnManualOrder_Click(object? sender, EventArgs e)
        {
            var settings = ConversionSettings.Load();
            var colorRegistry = ColorRegistry.Load(settings.ColorMap);
            var sizeRegistry = SizeRegistry.Load();
            var programNoRegistry = ProgramNoRegistry.Load();

            using var orderDialog = new MaOrForm(settings, colorRegistry, sizeRegistry, programNoRegistry);
            if (orderDialog.ShowDialog(this) != DialogResult.OK) return;

            // Dosya diyaloğu sormadan, %AppData%\TurcaExce\Programlar altında
            // bugünün tarihiyle bir klasör açar (örn. 20260729) ve içine Program
            // No adıyla kaydeder (örn. Programlar\20260729\PRG-123.xlsx).
            // Programın kurulu olduğu klasör kullanılmıyor: ClickOnce kurulum
            // yolu her güncellemede değiştiğinden dosyalara ulaşılamıyordu
            // (bkz. AppPaths.ProgramsDirectory).
            var now = DateTime.Now;
            var dateFolder = Path.Combine(AppPaths.ProgramsDirectory, now.ToString("yyyyMMdd"));
            Directory.CreateDirectory(dateFolder);

            var safeProgramNo = string.Concat(orderDialog.ProgramNo
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            var targetPath = Path.Combine(dateFolder, $"{safeProgramNo}.xlsx");

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Dönüştürülüyor...";

                var eanRegistry = EanRegistry.Load(settings.StartingEan);
                var serialRegistry = SerialRegistry.Load();
                var result = new ConversionService(settings, eanRegistry, colorRegistry, sizeRegistry, serialRegistry)
                    .ConvertManual(orderDialog.Quality, orderDialog.Rows);

                var summary = new ManualOrderSummary
                {
                    ProgramNo = orderDialog.ProgramNo,
                    Date = now,
                    TotalM2 = orderDialog.TotalM2,
                };
                ApplyConversionResult(result, eanRegistry, targetPath,
                    $"{orderDialog.Rows.Count} desen/renk satırı girildi, {result.Rows.Count} ürün satırı yazıldı, {targetPath} olarak kaydedildi",
                    summary);

                // Dönüşüm gerçekten üretildikten sonra işaretle; MaOrForm'da
                // yalnızca kontrol edilir, kalıcı kayıt burada yapılır.
                programNoRegistry.MarkUsed(orderDialog.ProgramNo);

                // Çıktı kullanıcının seçmediği bir klasöre yazıldığı için
                // klasör kendiliğinden açılır, dosya seçili gelir.
                RevealInExplorer(targetPath);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Hata oluştu.";
                MessageBox.Show(this, ex.Message, "Dönüşüm hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Çıktı Excel'ini varsayılan yazıcıya, dosyanın kendi içine gömülü
        /// yazdırma ayarlarıyla (yatay + tek sayfaya sığdır, bkz. ExcelWriter)
        /// doğrudan gönderir; kullanıcı her seferinde Excel'i açıp ayarları
        /// elle seçmek zorunda kalmaz.
        /// </summary>
        private void btnPrint_Click(object? sender, EventArgs e)
        {
            var path = txtTargetPath.Text;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "Önce bir üretim emri Excel'i oluşturun.", "Yazdırılacak dosya yok",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "print" });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Yazdırma başlatılamadı: {ex.Message}", "Yazdırma hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Dosyanın bulunduğu klasörü Windows Gezgini'nde açar ve dosyayı seçili
        /// gösterir. Dosya bu noktada zaten oluşturulduğundan buradaki bir hata
        /// dönüşümü başarısız saymaz; yalnızca durum satırında belirtilir.
        /// </summary>
        private void RevealInExplorer(string filePath)
        {
            try
            {
                // explorer.exe'nin /select sözdizimi tek argüman bekliyor
                // ("/select,\"yol\""), bu yüzden ArgumentList değil Arguments.
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"")
                {
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                lblStatus.Text += $" (klasör açılamadı: {ex.Message})";
            }
        }

        /// <summary>
        /// Bir ConversionResult'ı diske yazar, EAN kaydını kalıcılaştırır ve
        /// önizleme/uyarı/durum alanlarını günceller. Dosyadan okuma ve elle
        /// giriş akışlarının ikisi de sonunda burayı kullanır.
        /// </summary>
        private void ApplyConversionResult(ConversionResult result, EanRegistry eanRegistry,
            string targetPath, string statusPrefix, ManualOrderSummary? summary = null)
        {
            ExcelWriter.Write(targetPath, result.Rows, summary);
            eanRegistry.Save();

            txtTargetPath.Text = targetPath;
            _currentRows = new List<ProductEntryRow>(result.Rows);
            _currentSummary = summary;
            BindPreviewGrid();

            txtWarnings.Lines = [.. result.Warnings];
            lblStatus.Text = statusPrefix +
                (result.Warnings.Count > 0 ? $", {result.Warnings.Count} uyarı var." : ".");
        }

        /// <summary>
        /// gridPreview'u _currentRows'a bağlar, kolon başlıklarını çıktı
        /// dosyasındakilerle eşitler ve yalnızca Kalite Revize'nin kullandığı
        /// iç alanları (QualityCode, ProductCodeSuffix) gizler (çıktıya
        /// yazılmıyor, kullanıcıya gösterilecek bir bilgi değil).
        /// </summary>
        private void BindPreviewGrid()
        {
            gridPreview.DataSource = null;
            gridPreview.DataSource = _currentRows;

            for (int c = 0; c < ExcelWriter.Headers.Length && c < gridPreview.Columns.Count; c++)
                gridPreview.Columns[c].HeaderText = ExcelWriter.Headers[c];

            if (gridPreview.Columns["QualityCode"] is { } qualityCodeColumn)
                qualityCodeColumn.Visible = false;
            if (gridPreview.Columns["ProductCodeSuffix"] is { } productCodeSuffixColumn)
                productCodeSuffixColumn.Visible = false;
        }

        /// <summary>
        /// Aynı kalite kodunu farklı müşteriler farklı adla isteyebiliyor.
        /// Bu buton son dönüşümdeki her farklı kalite kodunu tek tek gezip
        /// (Kalite Kodu / mevcut Kalite Adı, Geç | Tamam) adı değiştirme
        /// imkanı verir; Tamam denen her kod için o koda ait tüm satırların
        /// Kalite kolonu VE UrunKodu'ndaki Kalite Adı segmenti güncellenir.
        /// UrunKodu'nun da güncellenmesi zorunlu: TurcaDesk tarafında ürün
        /// UrunKodu'na göre bulunuyor/oluşturuluyor ve barkod (EAN-13)
        /// doğrudan UrunKodu'ndan üretiliyor (bkz. SoFastEntryManager.
        /// GeStockProduct, ClassesGeneEA13.GenerateEA13) - segment aynı
        /// kalırsa farklı adlı iki müşterinin ürünü/barkodu çakışır. Sonunda
        /// güncel liste txtTargetPath'e yeniden yazılır. Genel eşleşme
        /// tablosuna (Kalite Listesi) dokunmaz - burada girilen ad yalnızca
        /// bu çıktıya özeldir.
        /// </summary>
        private void btnQualityRev_Click(object? sender, EventArgs e)
        {
            if (_currentRows == null || _currentRows.Count == 0)
            {
                MessageBox.Show(this, "Önce dönüşüm yapın; revize edilecek bir liste bulunamadı.",
                    "Eksik bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTargetPath.Text))
            {
                MessageBox.Show(this, "Çıktı dosyası yolu boş.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var codes = _currentRows
                .Select(r => r.QualityCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();

            int updated = 0;
            foreach (var code in codes)
            {
                var currentName = _currentRows
                    .First(r => string.Equals(r.QualityCode, code, StringComparison.OrdinalIgnoreCase))
                    .Quality;

                var newName = PromptQualityRevision(code, currentName);
                if (newName == null) continue; // Geç

                foreach (var row in _currentRows)
                    if (string.Equals(row.QualityCode, code, StringComparison.OrdinalIgnoreCase))
                    {
                        row.Quality = newName;
                        row.ProductCode = $"{TurkishText.ToAsciiUpper(newName)}_{row.ProductCodeSuffix}";
                    }

                updated++;
            }

            if (updated == 0)
            {
                lblStatus.Text = "Kalite revizesi yapılmadı.";
                return;
            }

            BindPreviewGrid();

            try
            {
                ExcelWriter.Write(txtTargetPath.Text, _currentRows, _currentSummary);
                lblStatus.Text = $"{updated} kalite güncellendi, {txtTargetPath.Text} güncellendi.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Kalite Revize kaydedilemedi.";
                MessageBox.Show(this, ex.Message, "Kaydetme hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Kalite Listesi'ni (Kod -> Ad, conversion_settings.json'daki
        /// QualityMap) görüntüleyip düzenlemek için QuaMaForm'u açar.
        /// "Kaydet" ile kapanırsa değişiklik zaten dosyaya yazılmış olur.
        /// </summary>
        private void btnQualityList_Click(object sender, EventArgs e)
        {
            var settings = ConversionSettings.Load();
            using var dialog = new QuaMaForm(settings);
            dialog.ShowDialog(this);
        }

        /// <summary>
        /// Kalite Revize'de tek bir kod için sorulan ekran: Kalite Kodu
        /// (salt okunur) ve düzenlenebilir Kalite Adı (mevcut adla dolu
        /// gelir). Tamam'da yazılan ad (değişmemiş olsa da) döner; Geç'te
        /// (veya pencere kapatılırsa) null döner ve o kod hiç dokunulmaz.
        /// </summary>
        private string? PromptQualityRevision(string qualityCode, string currentName)
        {
            using var dialog = new Form
            {
                Text = "Kalite Revize",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(380, 150),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
            };

            var lblCode = new Label
            {
                Text = $"Kalite Kodu : {qualityCode}",
                Location = new Point(12, 14),
                Size = new Size(356, 20),
            };
            var lblName = new Label
            {
                Text = "Kalite Adı :",
                Location = new Point(12, 48),
                Size = new Size(78, 23),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var txt = new TextBox { Location = new Point(96, 48), Size = new Size(272, 23), Text = currentName };
            var btnOk = new Button
            {
                Text = "Tamam",
                DialogResult = DialogResult.OK,
                Location = new Point(212, 104),
                Size = new Size(75, 28),
            };
            var btnSkip = new Button
            {
                Text = "Geç",
                DialogResult = DialogResult.Cancel,
                Location = new Point(293, 104),
                Size = new Size(75, 28),
            };

            dialog.Controls.AddRange([lblCode, lblName, txt, btnOk, btnSkip]);
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnSkip;

            return dialog.ShowDialog(this) == DialogResult.OK && txt.Text.Trim().Length > 0
                ? txt.Text.Trim()
                : null;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Dönüşüm sırasında tanınmayan renk kodu çıkınca adını sorar.
        /// Verilen ad colors.txt'ye kaydedilir; "Atla" denirse null döner ve
        /// kod aynen kullanılır.
        /// </summary>
        private string? PromptColorName(string colorCode)
        {
            using var dialog = new Form
            {
                Text = "Bilinmeyen renk kodu",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(380, 130),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
            };

            var lbl = new Label
            {
                Text = $"\"{colorCode}\" renk kodu tanınmadı.\nRenk adını yazın (colors.txt'ye kaydedilecek):",
                Location = new Point(12, 12),
                Size = new Size(356, 36),
            };
            var txt = new TextBox { Location = new Point(12, 54), Size = new Size(356, 23) };
            var btnOk = new Button
            {
                Text = "Kaydet",
                DialogResult = DialogResult.OK,
                Location = new Point(212, 90),
                Size = new Size(75, 28),
            };
            var btnSkip = new Button
            {
                Text = "Atla",
                DialogResult = DialogResult.Cancel,
                Location = new Point(293, 90),
                Size = new Size(75, 28),
            };

            dialog.Controls.AddRange([lbl, txt, btnOk, btnSkip]);
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnSkip;

            return dialog.ShowDialog(this) == DialogResult.OK && txt.Text.Trim().Length > 0
                ? txt.Text.Trim()
                : null;
        }

        /// <summary>
        /// Dönüşüm sırasında tanınmayan ebat kodu (160X230, 80X150 gibi
        /// normal 2-3 haneli kalıba uymayan, örn. 4X25) çıkınca gerçek ölçüyü
        /// sorar. Verilen ölçü sizes.txt'ye kaydedilir; "Atla" denirse null
        /// döner ve kod aynen kullanılır.
        /// </summary>
        private string? PromptSizeName(string sizeCode)
        {
            using var dialog = new Form
            {
                Text = "Bilinmeyen ebat kodu",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(380, 130),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
            };

            var lbl = new Label
            {
                Text = $"\"{sizeCode}\" ebat kodu tanınmadı.\nGerçek ölçüyü yazın (örn. 400x2500), sizes.txt'ye kaydedilecek:",
                Location = new Point(12, 12),
                Size = new Size(356, 36),
            };
            var txt = new TextBox { Location = new Point(12, 54), Size = new Size(356, 23) };
            var btnOk = new Button
            {
                Text = "Kaydet",
                DialogResult = DialogResult.OK,
                Location = new Point(212, 90),
                Size = new Size(75, 28),
            };
            var btnSkip = new Button
            {
                Text = "Atla",
                DialogResult = DialogResult.Cancel,
                Location = new Point(293, 90),
                Size = new Size(75, 28),
            };

            dialog.Controls.AddRange([lbl, txt, btnOk, btnSkip]);
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnSkip;

            return dialog.ShowDialog(this) == DialogResult.OK && txt.Text.Trim().Length > 0
                ? txt.Text.Trim()
                : null;
        }

        /// <summary>
        /// Dönüşüm sırasında tanınmayan kalite kodu (desen dosya adının
        /// prefix'i, örn. 72A) çıkınca adını sorar. Verilen ad Kalite
        /// Listesi'ne (ConversionSettings.QualityMap) kaydedilir; "Atla"
        /// denirse null döner ve kod aynen kullanılır.
        /// </summary>
        private string? PromptQualityName(string qualityCode)
        {
            using var dialog = new Form
            {
                Text = "Bilinmeyen kalite kodu",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(380, 130),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
            };

            var lbl = new Label
            {
                Text = $"Kalite kodu bulunamadı.\nKalite Kodu: {qualityCode}\nAltına Kalite Adı girin (Kalite Listesi'ne kaydedilecek):",
                Location = new Point(12, 12),
                Size = new Size(356, 48),
            };
            var txt = new TextBox { Location = new Point(12, 64), Size = new Size(356, 23) };
            var btnOk = new Button
            {
                Text = "Kaydet",
                DialogResult = DialogResult.OK,
                Location = new Point(212, 96),
                Size = new Size(75, 28),
            };
            var btnSkip = new Button
            {
                Text = "Atla",
                DialogResult = DialogResult.Cancel,
                Location = new Point(293, 96),
                Size = new Size(75, 28),
            };

            dialog.Controls.AddRange([lbl, txt, btnOk, btnSkip]);
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnSkip;

            return dialog.ShowDialog(this) == DialogResult.OK && txt.Text.Trim().Length > 0
                ? txt.Text.Trim()
                : null;
        }

        /// <summary>
        /// Gerçek bir Microsoft giriş penceresi açar (sistem tarayıcısında).
        /// Şifre hiç kullanılmaz; token exe klasöründeki şifrelenmiş
        /// msal_token_cache.bin dosyasında saklanır, bir daha giriş istemez.
        /// Azure AD Client ID conversion_settings.json → Email.AzureAdClientId
        /// alanına önceden yazılmış olmalıdır.
        /// </summary>
        private async void btnOutlookLogin_Click(object? sender, EventArgs e) => await SignInToOutlookAsync();

        /// <summary>true döner giriş başarılıysa; buton tıklaması ve otomatik yönlendirme ikisi de bunu kullanır.</summary>
        private async Task<bool> SignInToOutlookAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Microsoft giriş penceresi açılıyor...";

                var settings = ConversionSettings.Load();
                var account = await OutlookAuthService.SignInInteractiveAsync(settings.Email);
                settings.Save();

                UpdateMailStatusLabel(settings.Email);
                lblStatus.Text = $"Outlook girişi başarılı: {account}";
                return true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Outlook girişi başarısız.";
                MessageBox.Show(this, ex.Message, "Giriş hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private async void btnSendEmail_Click(object? sender, EventArgs e)
        {
            var recipient = txtEmail.Text.Trim();
            if (recipient.Length == 0 || !recipient.Contains('@'))
            {
                MessageBox.Show(this, "Geçerli bir alıcı e-posta adresi yazın.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(txtTargetPath.Text))
            {
                MessageBox.Show(this, "Önce dönüştürme yapın; gönderilecek çıktı dosyası bulunamadı.",
                    "Eksik bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Göndermeden önce Outlook girişi yapılmış mı kontrol et;
            // yoksa boşuna gönderim denemesin, doğrudan girişi başlat.
            var initialSettings = ConversionSettings.Load();
            if (!EmailSender.IsConfigured(initialSettings.Email))
            {
                MessageBox.Show(this,
                    "Henüz Outlook ile giriş yapılmamış. Şimdi Microsoft giriş penceresi açılacak.",
                    "Giriş gerekli", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (!await SignInToOutlookAsync()) return; // giriş başarısız/vazgeçildi
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "E-posta gönderiliyor...";

                // Alıcı değiştiyse kaydet ki bir dahaki açılışta hatırlansın.
                var settings = ConversionSettings.Load();
                if (settings.Email.Recipient != recipient)
                {
                    settings.Email.Recipient = recipient;
                    settings.Save();
                }

                var fileName = Path.GetFileName(txtTargetPath.Text);
                await EmailSender.SendAsync(settings.Email, recipient, txtTargetPath.Text,
                    subject: $"Ürün Girişi - {fileName}",
                    body: $"Ektedir: {fileName}\n\nBu e-posta TurcaExce tarafından gönderilmiştir.");

                // Gönderim başarılıysa geçmişe yaz ("Gönderim Geçmişi" butonu bunu
                // listeler). Yazma başarısız olsa bile e-posta gitmiş sayılır,
                // kullanıcı yalnızca durum satırında uyarılır.
                lblStatus.Text = EmailHistory.Append(fileName, recipient)
                    ? $"E-posta gönderildi: {recipient}"
                    : $"E-posta gönderildi: {recipient} (geçmiş kaydı yazılamadı)";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "E-posta gönderilemedi.";
                MessageBox.Show(this, ex.Message, "E-posta hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>Hangi dosyanın hangi tarihte gönderildiğini listeler (en yeni en başta).</summary>
        private void btnEmaHistory_Click(object? sender, EventArgs e)
        {
            using var dialog = new EmaHisForm();
            dialog.ShowDialog(this);
        }
    }
}
