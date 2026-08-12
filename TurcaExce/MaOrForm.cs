using TurcaExce.Config;
using TurcaExce.Models;
using TurcaExce.Services;

namespace TurcaExce
{
    /// <summary>
    /// Üretim emri Excel'i (123.xls) olmayan müşteriler için pivot tarzı elle
    /// giriş penceresi: satırlar Desen+Renk, sütunlar Ebat, hücreler adet.
    /// "Üretim Emrine Dönüştür"e basılınca Quality/Rows doldurulur ve
    /// DialogResult.OK döner; asıl Excel yazımı ve EAN kaydı MainForm'da
    /// (ConversionService.ConvertManual üzerinden) yapılır.
    /// </summary>
    public class MaOrForm : Form
    {
        private static readonly string[] DefaultSizes = ["100X150", "150X230", "200X300", "250X350", "300X400"];

        /// <summary>Kalite açılır listesindeki bir öğe; ekranda "Kod - Ad" gösterilir, Quality için Ad kullanılır.</summary>
        private sealed record QualityItem(string Kod, string Ad)
        {
            public override string ToString() => $"{Kod} - {Ad}";
        }

        private readonly ConversionSettings _settings;
        private readonly ColorRegistry _colorRegistry;
        private readonly SizeRegistry _sizeRegistry;
        private readonly SizeLayout _sizeLayout;
        private readonly ProgramNoRegistry _programNoRegistry;
        private readonly ComboBox _cmbQuality;
        private readonly TextBox _txtProgramNo;
        private readonly DataGridView _grid;
        private readonly Label _lblTotals;
        private readonly List<string> _sizeColumns = [];
        private readonly ContextMenuStrip _rowContextMenu;
        private int _contextMenuRowIndex = -1;

        public string Quality => (_cmbQuality.SelectedItem as QualityItem)?.Ad ?? "";
        public string ProgramNo => _txtProgramNo.Text.Trim();
        public List<ManualOrderRow> Rows { get; } = [];
        public double TotalM2 { get; private set; }

        public MaOrForm(ConversionSettings settings, ColorRegistry colorRegistry, SizeRegistry sizeRegistry,
            ProgramNoRegistry programNoRegistry)
        {
            _settings = settings;
            _colorRegistry = colorRegistry;
            _sizeRegistry = sizeRegistry;
            _sizeLayout = SizeLayout.Load();
            _programNoRegistry = programNoRegistry;

            // Hiç kayıtlı ebat yoksa (ilk kullanım), görseldeki gibi yaygın
            // beş ebatla başla; kalıcı kayda hemen yazılır (colors.txt'nin
            // ilk açılışta seed edilmesiyle aynı yaklaşım).
            if (_sizeRegistry.Map.Count == 0)
                foreach (var size in DefaultSizes)
                    _sizeRegistry.Add(size, size);

            Text = "Elle Üretim Emri Girişi";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ClientSize = new Size(1280, 720);
            MinimumSize = new Size(1180, 560);
            // Ebat sütunları yan yana çok yer kapladığı için pencere doğrudan
            // tam ekran açılır; kullanıcı isterse küçültebilir.
            WindowState = FormWindowState.Maximized;
            ShowInTaskbar = false;

            var lblQuality = new Label { Text = "Kalite:", Location = new Point(12, 15), AutoSize = true };
            _cmbQuality = new ComboBox
            {
                Location = new Point(60, 11),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            RefreshQualityItems();

            var btnQualityManage = new Button { Text = "Kalite Oluştur", Location = new Point(250, 10), Size = new Size(110, 25) };
            btnQualityManage.Click += (_, _) =>
            {
                using var manager = new QuaMaForm(_settings);
                if (manager.ShowDialog(this) == DialogResult.OK)
                    RefreshQualityItems();
            };

            var lblProgramNo = new Label
            {
                Text = "Program No:",
                Location = new Point(1060, 15),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _txtProgramNo = new TextBox
            {
                Location = new Point(1148, 11),
                Size = new Size(120, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            _rowContextMenu = new ContextMenuStrip();
            var menuAddImage = new ToolStripMenuItem("Resim Ekle...");
            menuAddImage.Click += (_, _) => AddImageToRow(_contextMenuRowIndex);
            var menuRemoveImage = new ToolStripMenuItem("Resim Kaldır");
            menuRemoveImage.Click += (_, _) => RemoveImageFromRow(_contextMenuRowIndex);
            _rowContextMenu.Items.AddRange([menuAddImage, menuRemoveImage]);

            _grid = new DataGridView
            {
                Location = new Point(12, 44),
                Size = new Size(1256, 560),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            };
            // Yol: Excel akışındaki (ConversionService.Convert) source.PathNo'nun elle
            // giriş karşılığı — aynı üretim emri içinde satır bazında değişebilir
            // (örn. 3 satır Yol 1, ardından 3 satır Yol 2 girilebilir), bu yüzden tek
            // bir üst alan değil, Excel'deki gibi satır başına bir kolon.
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Yol",
                HeaderText = "Yol",
                FillWeight = 35,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Desen", HeaderText = "Desen", FillWeight = 80 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Renk", HeaderText = "Renk", FillWeight = 80 });
            // Kenar, Desen+Renk ile birlikte satır bazında değişir (sabit
            // gömülü liste — bkz. EdgeOptions); serbest metin kabul edilmez.
            var kenarColumn = new DataGridViewComboBoxColumn
            {
                Name = "Kenar",
                HeaderText = "Kenar",
                FillWeight = 100,
                FlatStyle = FlatStyle.Flat,
            };
            kenarColumn.Items.AddRange([.. EdgeOptions.All.Select(o => o.Name)]);
            _grid.Columns.Add(kenarColumn);
            // Resim: yalnızca görüntüleme amaçlı (dosya adı), sağ tık menüsüyle
            // doldurulur; elle düzenlenemez. Gerçek yol satırın Tag'inde tutulur.
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Resim",
                HeaderText = "Resim",
                FillWeight = 70,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { ForeColor = SystemColors.GrayText },
            });
            _grid.EditingControlShowing += Grid_EditingControlShowing;
            _grid.CellValidating += Grid_CellValidating;
            _grid.CellValueChanged += (_, _) => UpdateTotals();
            _grid.DataError += (_, args) => args.ThrowException = false;
            _grid.CellMouseDown += Grid_CellMouseDown;
            _grid.CellDoubleClick += (_, args) =>
            {
                if (args.RowIndex >= 0 && _grid.Columns[args.ColumnIndex].Name == "Resim")
                    AddImageToRow(args.RowIndex);
            };

            // Kayıtlı ebatların tamamı yan yana sığmadığı için ekran, en son
            // kaydedilen şablonla (size_layout.txt) açılır; hiç şablon yoksa
            // yalnızca yaygın beş ebatla. Kalanları kullanıcı "Kayıtlı Ebat
            // Ekle" ile getirir, istemediğini "Sütun Ebat Sil" ile kaldırır.
            List<string> initialSizes = _sizeLayout.Sizes.Count > 0 ? _sizeLayout.Sizes : [.. DefaultSizes];
            foreach (var size in initialSizes)
                AddSizeColumn(size);

            var btnAddRow = new Button { Text = "+ Satır Ekle", Location = new Point(372, 10), Size = new Size(95, 25) };
            btnAddRow.Click += (_, _) => AddGridRow();

            var btnRemoveRow = new Button { Text = "Satır Sil", Location = new Point(471, 10), Size = new Size(80, 25) };
            btnRemoveRow.Click += (_, _) => RemoveSelectedRows();

            var btnAddSize = new Button { Text = "Ebat Ekle", Location = new Point(555, 10), Size = new Size(85, 25) };
            btnAddSize.Click += (_, _) => AddSizeColumnInteractive();

            var btnAddSavedSize = new Button { Text = "Kayıtlı Ebat Ekle", Location = new Point(644, 10), Size = new Size(125, 25) };
            btnAddSavedSize.Click += (_, _) => AddSavedSizeColumns();

            var btnRemoveSize = new Button { Text = "Sütun Ebat Sil", Location = new Point(773, 10), Size = new Size(110, 25) };
            btnRemoveSize.Click += (_, _) => RemoveSizeColumns();

            _lblTotals = new Label
            {
                Text = "Toplam Adet: 0    Toplam m²: 0",
                Location = new Point(12, 612),
                Size = new Size(560, 23),
                ForeColor = SystemColors.GrayText,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };

            var btnCancel = new Button
            {
                Text = "İptal",
                Location = new Point(971, 678),
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnOk = new Button
            {
                Text = "Üretim Emrine Dönüştür",
                Location = new Point(1052, 678),
                Size = new Size(216, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            btnOk.Click += BtnOk_Click;

            Controls.AddRange([lblQuality, _cmbQuality, btnQualityManage, lblProgramNo, _txtProgramNo,
                btnAddRow, btnRemoveRow, btnAddSize, btnAddSavedSize, btnRemoveSize, _grid, _lblTotals, btnCancel, btnOk]);
            CancelButton = btnCancel;

            for (int i = 0; i < 3; i++) AddGridRow();
        }

        /// <summary>
        /// Yeni satır ekler; Kenar hücresini varsayılan "Saçak" ile, Yol hücresini
        /// ise bir önceki satırdaki değerle (yoksa "1" ile) doldurur — art arda
        /// aynı yola ait birkaç satır girerken her seferinde yeniden yazmaya gerek
        /// kalmaz, yol değişince kullanıcı yalnızca o satırda değeri günceller.
        /// </summary>
        private void AddGridRow()
        {
            var previousYol = _grid.Rows.Count > 0 ? _grid.Rows[^1].Cells["Yol"].Value?.ToString() : null;

            var index = _grid.Rows.Add();
            _grid.Rows[index].Cells["Yol"].Value = string.IsNullOrWhiteSpace(previousYol) ? "1" : previousYol;
            _grid.Rows[index].Cells["Kenar"].Value = "Saçak";
        }

        /// <summary>Kalite listesini _settings.QualityMap'ten yeniden yükler; önceden seçili Kod korunur.</summary>
        private void RefreshQualityItems()
        {
            var previousKod = (_cmbQuality.SelectedItem as QualityItem)?.Kod;

            _cmbQuality.Items.Clear();
            foreach (var kv in _settings.QualityMap.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                _cmbQuality.Items.Add(new QualityItem(kv.Key, kv.Value));

            if (previousKod != null)
                _cmbQuality.SelectedItem = _cmbQuality.Items.Cast<QualityItem>()
                    .FirstOrDefault(q => q.Kod == previousKod);
        }

        private void RemoveSelectedRows()
        {
            foreach (DataGridViewRow row in _grid.SelectedRows.Cast<DataGridViewRow>().OrderByDescending(r => r.Index))
                if (!row.IsNewRow) _grid.Rows.Remove(row);
            UpdateTotals();
        }

        /// <summary>Sağ tıklanan satırı seçip "Resim Ekle/Kaldır" menüsünü orada açar.</summary>
        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;

            _grid.ClearSelection();
            _grid.Rows[e.RowIndex].Selected = true;
            _contextMenuRowIndex = e.RowIndex;
            _rowContextMenu.Show(_grid, _grid.PointToClient(MousePosition));
        }

        /// <summary>
        /// Halı resmi (bmp veya başka bir görsel) seçtirir; gerçek dosya yolu
        /// satırın Tag'inde, "Resim" kolonunda yalnızca dosya adı gösterilir.
        /// </summary>
        private void AddImageToRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _grid.Rows.Count || _grid.Rows[rowIndex].IsNewRow) return;

            using var dialog = new OpenFileDialog
            {
                Title = "Halı Resmi Seç",
                Filter = "Resim dosyaları (*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tif;*.tiff)|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tif;*.tiff|Tüm dosyalar (*.*)|*.*",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var row = _grid.Rows[rowIndex];
            row.Tag = dialog.FileName;
            row.Cells["Resim"].Value = Path.GetFileName(dialog.FileName);
        }

        private void RemoveImageFromRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _grid.Rows.Count || _grid.Rows[rowIndex].IsNewRow) return;

            var row = _grid.Rows[rowIndex];
            row.Tag = null;
            row.Cells["Resim"].Value = null;
        }

        /// <summary>
        /// Desen hücresi büyük harfle yazılır; Renk hücresinde bilinen renk
        /// adları öneri olarak sunulur (serbest metin de kabul edilir); Kenar
        /// hücresi yalnızca sabit listeden seçime izin verir.
        /// </summary>
        private void Grid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            var columnName = _grid.CurrentCell?.OwningColumn?.Name;

            if (columnName == "Kenar" && e.Control is ComboBox cb)
            {
                cb.DropDownStyle = ComboBoxStyle.DropDownList;
                return;
            }

            if (e.Control is not TextBox tb) return;

            tb.CharacterCasing = columnName == "Desen" ? CharacterCasing.Upper : CharacterCasing.Normal;
            if (columnName != "Renk") return;

            tb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
            var source = new AutoCompleteStringCollection();
            source.AddRange([.. _colorRegistry.Map.Values.Distinct()]);
            tb.AutoCompleteCustomSource = source;
        }

        /// <summary>Ebat kolonlarına yalnızca boş ya da tam sayı adet, Yol kolonuna yalnızca boş ya da pozitif tam sayı girilebilir.</summary>
        private void Grid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            var columnName = _grid.Columns[e.ColumnIndex].Name;
            var isSizeColumn = columnName.StartsWith("size_", StringComparison.Ordinal);
            if (!isSizeColumn && columnName != "Yol") return;

            var text = e.FormattedValue?.ToString()?.Trim() ?? "";
            if (text.Length == 0) return;

            var minValue = isSizeColumn ? 0 : 1;
            if (!int.TryParse(text, out var value) || value < minValue)
            {
                e.Cancel = true;
                _grid.Rows[e.RowIndex].ErrorText = isSizeColumn
                    ? "Ebat hücrelerine yalnızca adet (tam sayı) girin."
                    : "Yol hücresine yalnızca pozitif tam sayı girin.";
            }
            else
            {
                _grid.Rows[e.RowIndex].ErrorText = "";
            }
        }

        private void AddSizeColumnInteractive()
        {
            var input = PromptText("Yeni Ebat", "Ebatı yazın (örn. 400x2500):");
            if (string.IsNullOrWhiteSpace(input)) return;

            var normalized = ConversionService.NormalizeSize(input);
            if (_sizeColumns.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Bu ebat zaten ekli.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _sizeRegistry.Add(normalized, normalized);
            AddSizeColumn(normalized);
            SaveSizeLayout();
        }

        /// <summary>
        /// Daha önce kaydedilmiş ebatlardan (sizes.txt) ekranda olmayanları
        /// listeler; seçilenler sütun olarak eklenir.
        /// </summary>
        private void AddSavedSizeColumns()
        {
            var available = _sizeRegistry.Map.Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(size => !_sizeColumns.Contains(size, StringComparer.OrdinalIgnoreCase))
                .OrderBy(SizeSortKey)
                .ThenBy(size => size, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (available.Count == 0)
            {
                MessageBox.Show(this, "Kayıtlı ebatların tamamı zaten ekli.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = PromptSelect("Kayıtlı Ebat Ekle", "Eklemek istediğiniz ebatları seçin:", available);
            if (selected.Count == 0) return;

            foreach (var size in selected)
                AddSizeColumn(size);
            SaveSizeLayout();
        }

        /// <summary>
        /// Yalnızca ekrandaki ebat sütununu kaldırır; kayıtlı ebat listesi
        /// (sizes.txt) olduğu gibi kalır, "Kayıtlı Ebat Ekle" ile geri getirilebilir.
        /// </summary>
        private void RemoveSizeColumns()
        {
            if (_sizeColumns.Count == 0)
            {
                MessageBox.Show(this, "Ekranda kaldırılacak ebat sütunu yok.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selected = PromptSelect("Sütun Ebat Sil", "Kaldırılacak ebat sütunlarını seçin:", _sizeColumns);
            if (selected.Count == 0) return;

            foreach (var size in selected)
            {
                _grid.Columns.Remove("size_" + size);
                _sizeColumns.RemoveAll(s => string.Equals(s, size, StringComparison.OrdinalIgnoreCase));
            }

            SaveSizeLayout();
            UpdateTotals();
        }

        /// <summary>Ekrandaki ebat sütunlarını şablon olarak kaydeder; pencere bir dahaki açılışta aynı listeyle gelir.</summary>
        private void SaveSizeLayout() => _sizeLayout.Save(_sizeColumns);

        /// <summary>"100X150" -> (100, 150); sıralamada "80X150" metin olarak "100X150"nin önüne geçmesin diye.</summary>
        private static (double W, double H) SizeSortKey(string size)
        {
            var parts = size.Split('X', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !double.TryParse(parts[0], out var w) || !double.TryParse(parts[1], out var h))
                return (double.MaxValue, double.MaxValue);
            return (w, h);
        }

        private void AddSizeColumn(string size)
        {
            if (_sizeColumns.Contains(size, StringComparer.OrdinalIgnoreCase)) return;
            _sizeColumns.Add(size);
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "size_" + size,
                HeaderText = size,
                FillWeight = 55,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
            });
        }

        private void UpdateTotals()
        {
            int totalPieces = 0;
            double totalM2 = 0;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                foreach (var sizeKey in _sizeColumns)
                {
                    var value = row.Cells["size_" + sizeKey].Value?.ToString();
                    if (!int.TryParse(value, out var qty) || qty <= 0) continue;

                    totalPieces += qty;
                    totalM2 += qty * ComputeM2(sizeKey);
                }
            }

            _lblTotals.Text = $"Toplam Adet: {totalPieces}    Toplam m²: {totalM2:0.##}";
        }

        /// <summary>"300X400" -> 3.00 x 4.00 = 12 m² (kolon başlığı santimetre, tek parça alanı).</summary>
        private static double ComputeM2(string sizeKey)
        {
            var parts = sizeKey.Split('X', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return 0;
            if (!double.TryParse(parts[0], out var w) || !double.TryParse(parts[1], out var h)) return 0;
            return (w / 100.0) * (h / 100.0);
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (_cmbQuality.SelectedItem == null)
            {
                MessageBox.Show(this, "Kalite seçin. Listede yoksa önce \"Kalite Oluştur\" ile ekleyin.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var programNo = ProgramNo;
            if (programNo.Length == 0)
            {
                MessageBox.Show(this, "Program No girin.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_programNoRegistry.IsUsed(programNo))
            {
                MessageBox.Show(this,
                    $"\"{programNo}\" program numarasıyla bu programı zaten çalıştırdınız. Aynı program numarasıyla tekrar kaydedilemez.",
                    "Program zaten çalıştırıldı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BuildRows();
            if (Rows.Count == 0)
            {
                MessageBox.Show(this, "En az bir satırda Desen, Renk ve bir ebat için adet girin.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BuildRows()
        {
            Rows.Clear();
            double totalM2 = 0;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;

                var pattern = row.Cells["Desen"].Value?.ToString()?.Trim() ?? "";
                var color = row.Cells["Renk"].Value?.ToString()?.Trim() ?? "";
                var edge = row.Cells["Kenar"].Value?.ToString()?.Trim();
                if (pattern.Length == 0 || color.Length == 0) continue;
                if (string.IsNullOrEmpty(edge)) edge = "Saçak";

                var yolText = row.Cells["Yol"].Value?.ToString()?.Trim();
                if (!int.TryParse(yolText, out var road) || road < 1) road = 1;

                var manualRow = new ManualOrderRow { Road = road, Pattern = pattern, Color = color, Edge = edge, ImagePath = row.Tag as string };
                foreach (var sizeKey in _sizeColumns)
                {
                    var value = row.Cells["size_" + sizeKey].Value?.ToString();
                    if (int.TryParse(value, out var qty) && qty > 0)
                        manualRow.Quantities[sizeKey] = qty;
                }

                if (manualRow.Quantities.Count > 0)
                {
                    Rows.Add(manualRow);
                    foreach (var (sizeKey, qty) in manualRow.Quantities)
                        totalM2 += qty * ComputeM2(sizeKey);
                }
            }
            TotalM2 = totalM2;
        }

        private string? PromptText(string title, string message)
        {
            using var dialog = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(320, 120),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
            };
            var lbl = new Label { Text = message, Location = new Point(12, 12), Size = new Size(296, 20) };
            var txt = new TextBox { Location = new Point(12, 38), Size = new Size(296, 23) };
            var btnOk = new Button
            {
                Text = "Tamam",
                DialogResult = DialogResult.OK,
                Location = new Point(152, 78),
                Size = new Size(75, 28),
            };
            var btnCancel = new Button
            {
                Text = "İptal",
                DialogResult = DialogResult.Cancel,
                Location = new Point(233, 78),
                Size = new Size(75, 28),
            };
            dialog.Controls.AddRange([lbl, txt, btnOk, btnCancel]);
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnCancel;

            return dialog.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
        }

        /// <summary>Çoklu seçimli basit liste penceresi; işaretlenenleri döndürür (iptalde boş liste).</summary>
        private List<string> PromptSelect(string title, string message, IEnumerable<string> items)
        {
            using var dialog = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(320, 360),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
            };
            var lbl = new Label { Text = message, Location = new Point(12, 12), Size = new Size(296, 20) };
            var list = new CheckedListBox
            {
                Location = new Point(12, 38),
                Size = new Size(296, 262),
                CheckOnClick = true,
            };
            list.Items.AddRange([.. items]);
            var btnOk = new Button
            {
                Text = "Tamam",
                DialogResult = DialogResult.OK,
                Location = new Point(152, 318),
                Size = new Size(75, 28),
            };
            var btnCancel = new Button
            {
                Text = "İptal",
                DialogResult = DialogResult.Cancel,
                Location = new Point(233, 318),
                Size = new Size(75, 28),
            };
            dialog.Controls.AddRange([lbl, list, btnOk, btnCancel]);
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnCancel;

            return dialog.ShowDialog(this) == DialogResult.OK
                ? [.. list.CheckedItems.Cast<string>()]
                : [];
        }
    }
}
