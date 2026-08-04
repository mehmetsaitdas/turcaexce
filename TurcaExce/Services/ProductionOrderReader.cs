using System.Globalization;
using System.Text.RegularExpressions;
using TurcaExce.Models;
using NPOI.SS.UserModel;

namespace TurcaExce.Services
{
    /// <summary>
    /// 123.xls üretim emrini okur. Dosya yazıcı çıktısı gibi düzenlendiğinden
    /// (birleşik hücreler, sayfa sonlarında araya giren TOPLAM/footer satırları)
    /// sabit satır aralıklarına güvenilmez: veri satırı, içinde ".ep" ile biten
    /// bir hücre olmasından tanınır; "YOL NO = n" hücresi de o andaki yol
    /// numarasını günceller.
    /// </summary>
    public static partial class ProductionOrderReader
    {
        [GeneratedRegex(@"YOL\s*NO\s*=\s*(\d+)", RegexOptions.IgnoreCase)]
        private static partial Regex PathNoRegex();

        public static List<ProductionOrderLine> Read(string filePath)
        {
            var lines = new List<ProductionOrderLine>();

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var workbook = WorkbookFactory.Create(fs);
            var sheet = workbook.GetSheetAt(0);

            int pathNo = 0;

            for (int r = sheet.FirstRowNum; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                int epColumn = -1;
                string patternFileName = "";

                foreach (var cell in row.Cells)
                {
                    if (cell.CellType != CellType.String) continue;

                    var text = cell.StringCellValue.Trim();
                    var pathMatch = PathNoRegex().Match(text);
                    if (pathMatch.Success)
                        pathNo = int.Parse(pathMatch.Groups[1].Value);

                    if (text.EndsWith(".ep", StringComparison.OrdinalIgnoreCase))
                    {
                        patternFileName = text;
                        epColumn = cell.ColumnIndex;
                    }
                }

                if (epColumn < 0) continue;

                // .ep hücresinin sağındaki sayısal hücreler sırasıyla:
                // S.AD | D.ADET | Boy (Metre) | Atkı | Yol Uzunluğu
                var right = CollectNumbers(row, c => c > epColumn);
                // Sıra no: .ep hücresinin solundaki son sayı
                var left = CollectNumbers(row, c => c < epColumn);

                lines.Add(new ProductionOrderLine
                {
                    PathNo = pathNo,
                    ItemNo = left.Count > 0 ? (int)left[^1] : 0,
                    PatternFileName = patternFileName,
                    SCount = right.Count > 0 ? (int)right[0] : 0,
                    Quantity = right.Count > 1 ? Math.Max(1, (int)right[1]) : 1,
                    LengthMeters = right.Count > 2 ? right[2] : 0,
                    WeftCount = right.Count > 3 ? right[3] : 0,
                    PathLength = right.Count > 4 ? right[4] : 0,
                });
            }

            return lines;
        }

        private static List<double> CollectNumbers(IRow row, Func<int, bool> columnFilter)
        {
            var numbers = new List<double>();
            foreach (var cell in row.Cells)
            {
                if (!columnFilter(cell.ColumnIndex)) continue;

                if (cell.CellType == CellType.Numeric)
                    numbers.Add(cell.NumericCellValue);
                else if (cell.CellType == CellType.String &&
                         double.TryParse(cell.StringCellValue.Trim().Replace(',', '.'),
                             NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    numbers.Add(value);
            }
            return numbers;
        }
    }
}
