using TurcaExce.Services;

namespace TurcaExce
{
    /// <summary>
    /// E-posta gönderim geçmişini (email_history.txt) EN YENİ EN BAŞTA olacak
    /// şekilde listeler. Üstteki arama alanı yazıldıkça tarih ve dosya adına
    /// göre satırları süzer. Kayıtlar yalnızca görüntülenir; ekran üzerinden
    /// değiştirilemez.
    /// </summary>
    public class EmaHisForm : Form
    {
        /// <summary>Ekranda görünen tarih biçimi; arama da bu metin üzerinde yapılır.</summary>
        private const string DisplayFormat = "dd.MM.yyyy HH:mm";

        private readonly List<EmailHistoryEntry> _entries;
        private readonly TextBox _txtSearch;
        private readonly DataGridView _grid;
        private readonly Label _lblInfo;

        public EmaHisForm()
        {
            _entries = EmailHistory.Load();

            Text = "E-posta Gönderim Geçmişi";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            ClientSize = new Size(560, 460);
            MinimumSize = new Size(460, 320);

            _txtSearch = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(536, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "Ara (tarih veya dosya adı)",
            };
            _txtSearch.TextChanged += (_, _) => Populate();

            _grid = new DataGridView
            {
                Location = new Point(12, 45),
                Size = new Size(536, 366),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
            };
            // Tarih hücresi metin değil DateTime tutuyor: hem "dd.MM.yyyy HH:mm"
            // olarak görünsün hem de başlığa tıklanınca gerçek tarih sırasına
            // göre sıralanabilsin (metin olsaydı alfabetik sıralanırdı).
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Tarih",
                HeaderText = "Tarih Saati",
                ValueType = typeof(DateTime),
                FillWeight = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = DisplayFormat },
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Dosya",
                HeaderText = "Dosya Adı",
                FillWeight = 130,
            });

            _lblInfo = new Label
            {
                Location = new Point(12, 420),
                Size = new Size(455, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = SystemColors.GrayText,
                AutoEllipsis = true,
            };

            var btnClose = new Button
            {
                Text = "Kapat",
                Location = new Point(473, 418),
                Size = new Size(75, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnClose.Click += (_, _) => Close();

            Controls.AddRange([_txtSearch, _grid, _lblInfo, btnClose]);
            CancelButton = btnClose;

            Populate();
        }

        /// <summary>Arama alanındaki metne uyan kayıtları tabloya yazar.</summary>
        private void Populate()
        {
            var query = _txtSearch.Text.Trim();

            _grid.Rows.Clear();
            foreach (var entry in _entries)
            {
                if (query.Length > 0 && !Matches(entry, query)) continue;
                _grid.Rows.Add(entry.SentAt, entry.FileName);
            }
            _grid.ClearSelection();

            _lblInfo.Text = _entries.Count == 0
                ? $"Henüz gönderim kaydı yok   |   {EmailHistory.FilePath}"
                : _grid.Rows.Count == _entries.Count
                    ? $"{_entries.Count} kayıt   |   {EmailHistory.FilePath}"
                    : $"{_grid.Rows.Count} / {_entries.Count} kayıt   |   {EmailHistory.FilePath}";
        }

        /// <summary>
        /// Ekranda görünen iki alanda da (tarih metni ve dosya adı) arar.
        /// Karşılaştırma kullanıcının kültürüne göre büyük/küçük harf duyarsız
        /// yapılır ki Türkçe "İ/ı" ayrımı aramayı bozmasın.
        /// </summary>
        private static bool Matches(EmailHistoryEntry entry, string query) =>
            entry.SentAt.ToString(DisplayFormat).Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
            entry.FileName.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }
}
