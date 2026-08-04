using System.Diagnostics;
using TurcaExce.Config;
using TurcaExce.Models;
using TurcaExce.Services;

namespace TurcaExce
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            // Pencere ikonu = exe'ye gömülü uygulama ikonu (Resources\app.ico).
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            lblSettingsFile.Text =
                $"Eşleşme tabloları: {ConversionSettings.FilePath}   |   Renkler: {ColorRegistry.FilePath}   |   EAN kayıtları: {EanRegistry.FilePath}";

            var settings = ConversionSettings.Load();

            // İlk açılışta koda gömülü statik renkler colors.txt'ye yazılır;
            // sonraki açılışlarda renkler bu dosyadan okunur.
            ColorRegistry.Load(settings.ColorMap);

            // Kayıtlı alıcı adresini geri yükle.
            txtEmail.Text = settings.Email.Recipient;
            UpdateMailStatusLabel(settings.Email);

            InitializeCompanyName();
        }

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
                var sourceLines = ProductionOrderReader.Read(txtSourcePath.Text);
                var result = new ConversionService(settings, eanRegistry, colorRegistry, sizeRegistry,
                        PromptColorName, PromptSizeName)
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

            // Dosya diyaloğu sormadan, programın bulunduğu klasör altında bugünün
            // tarihiyle bir klasör açar (örn. 20260729) ve içine Program No adıyla
            // kaydeder (örn. 20260729\PRG-123.xlsx).
            var now = DateTime.Now;
            var dateFolder = Path.Combine(AppContext.BaseDirectory, now.ToString("yyyyMMdd"));
            Directory.CreateDirectory(dateFolder);

            var safeProgramNo = string.Concat(orderDialog.ProgramNo
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            var targetPath = Path.Combine(dateFolder, $"{safeProgramNo}.xlsx");

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Dönüştürülüyor...";

                var eanRegistry = EanRegistry.Load(settings.StartingEan);
                var result = new ConversionService(settings, eanRegistry, colorRegistry, sizeRegistry)
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
            gridPreview.DataSource = new List<ProductEntryRow>(result.Rows);
            // Önizleme kolon başlıklarını çıktı dosyasındakilerle eşitle.
            for (int c = 0; c < ExcelWriter.Headers.Length && c < gridPreview.Columns.Count; c++)
                gridPreview.Columns[c].HeaderText = ExcelWriter.Headers[c];

            txtWarnings.Lines = [.. result.Warnings];
            lblStatus.Text = statusPrefix +
                (result.Warnings.Count > 0 ? $", {result.Warnings.Count} uyarı var." : ".");
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

                lblStatus.Text = $"E-posta gönderildi: {recipient}";
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
    }
}
