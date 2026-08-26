using TurcaExce.Config;

namespace TurcaExce
{
    /// <summary>
    /// Kalite kod/ad listesini yönetir (örn. "16/2" -> "HAAB"). Liste
    /// ConversionSettings.QualityMap'te tutulur; "Kaydet"e basılınca
    /// conversion_settings.json'a yazılır. Elle üretim emri girişindeki
    /// Kalite açılır listesi buradan beslenir.
    /// </summary>
    public class QuaMaForm : Form
    {
        private readonly ConversionSettings _settings;
        private readonly DataGridView _grid;
        private readonly TextBox _txtSearch;

        public QuaMaForm(ConversionSettings settings)
        {
            _settings = settings;

            Text = "Kalite Listesi";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(620, 560);

            var lblHint = new Label
            {
                Text = "Kod = seride/desende kullanılan kısa kod, Ad = çıktıda görünecek kalite adı.",
                Location = new Point(12, 10),
                Size = new Size(596, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = SystemColors.GrayText,
            };

            _txtSearch = new TextBox
            {
                Location = new Point(12, 46),
                Size = new Size(596, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "Ara (Kod veya Ad)...",
            };
            _txtSearch.TextChanged += (_, _) => ApplyFilter();

            _grid = new DataGridView
            {
                Location = new Point(12, 75),
                Size = new Size(596, 395),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kod", HeaderText = "Kod (örn. 16/2)", FillWeight = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ad", HeaderText = "Ad (örn. HAAB)", FillWeight = 140 });
            _grid.DataError += (_, args) => args.ThrowException = false;

            foreach (var kv in _settings.QualityMap.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                _grid.Rows.Add(kv.Key, kv.Value);

            var btnAdd = new Button
            {
                Text = "+ Ekle",
                Location = new Point(12, 481),
                Size = new Size(90, 25),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            btnAdd.Click += (_, _) =>
            {
                // Yeni satır her zaman görünsün diye filtre temizlenir; aksi
                // halde arama terimine uymayan boş satır anında gizlenirdi.
                _txtSearch.Text = "";
                var row = _grid.Rows[_grid.Rows.Add()];
                _grid.CurrentCell = row.Cells["Kod"];
                _grid.BeginEdit(true);
            };

            var btnRemove = new Button
            {
                Text = "Sil",
                Location = new Point(108, 481),
                Size = new Size(90, 25),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            btnRemove.Click += (_, _) =>
            {
                foreach (DataGridViewRow row in _grid.SelectedRows.Cast<DataGridViewRow>().OrderByDescending(r => r.Index))
                    if (!row.IsNewRow) _grid.Rows.Remove(row);
            };

            var btnCancel = new Button
            {
                Text = "İptal",
                Location = new Point(450, 518),
                Size = new Size(75, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnSave = new Button
            {
                Text = "Kaydet",
                Location = new Point(533, 518),
                Size = new Size(75, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnSave.Click += BtnSave_Click;

            Controls.AddRange([lblHint, _txtSearch, _grid, btnAdd, btnRemove, btnCancel, btnSave]);
            CancelButton = btnCancel;
        }

        /// <summary>
        /// _txtSearch'teki terimi Kod veya Ad'da (büyük/küçük harf duyarsız)
        /// içeren satırları gösterir, diğerlerini gizler. Sadece görünürlüğü
        /// değiştirir; BtnSave_Click gizli satırları da okuduğundan filtre
        /// kaydı etkilemez.
        /// </summary>
        private void ApplyFilter()
        {
            var term = _txtSearch.Text.Trim();

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;

                if (term.Length == 0)
                {
                    row.Visible = true;
                    continue;
                }

                var kod = row.Cells["Kod"].Value?.ToString() ?? "";
                var ad = row.Cells["Ad"].Value?.ToString() ?? "";
                row.Visible = kod.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                              ad.Contains(term, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            var map = new Dictionary<string, string>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;

                var kod = row.Cells["Kod"].Value?.ToString()?.Trim() ?? "";
                var ad = row.Cells["Ad"].Value?.ToString()?.Trim() ?? "";
                if (kod.Length == 0 || ad.Length == 0) continue;

                map[kod] = ad;
            }

            if (map.Count == 0)
            {
                MessageBox.Show(this, "En az bir Kod/Ad çifti girin.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings.QualityMap.Clear();
            foreach (var kv in map)
                _settings.QualityMap[kv.Key] = kv.Value;
            _settings.Save();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void InitializeComponent()
        {

        }
    }
}
