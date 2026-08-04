using TurcaExce.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace TurcaExce.Services
{
    /// <summary>Ürün giriş satırlarını Excel_Girisi.xlsx formatında yazar.</summary>
    public static class ExcelWriter
    {
        // Hedef dosyadaki kolon başlıkları (Excel_Girisi.xlsx ile birebir aynı,
        // sona eklenen "Resim" halı fotoğrafının gömüldüğü kolondur).
        public static readonly string[] Headers =
            ["Yol", "UrunKodu", "Kalite", "Desen", "Renk", "Ebat", "Kenar", "Ser", "EAN", "Resim"];

        // AutoSizeColumn, NPOI'nin SkiaSharp bağımlılığını gerektirdiğinden
        // sabit genişlik kullanılır (1 birim = 1/256 karakter).
        private static readonly int[] ColumnWidths = [6, 30, 10, 10, 12, 12, 18, 12, 15, 20];

        // Resim kolonunun index'i (Headers'ta "Resim"in konumuyla aynı).
        private const int ImageColumnIndex = 9;

        // Bir resmin kapladığı satır sayısı (gömülü resmin görsel yüksekliği için).
        private const int ImageRowSpan = 6;

        // Elle üretim emrinde toplam bilgisi kutusunun yazıldığı kolon ve genişliği;
        // Resim ile arasında bir boş kolon (ImageColumnIndex + 1) bırakılır.
        private const int SummaryColumnIndex = ImageColumnIndex + 2;
        private const int SummaryColumnWidth = 28;

        public static void Write(string filePath, IReadOnlyList<ProductEntryRow> rows, ManualOrderSummary? summary = null)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sayfa1");

            // Yazdırma varsayılanları: yatay yönlendirme + tüm tabloyu tek
            // sayfaya sığdır. Böylece dosya Excel'den yazdırılırken (veya
            // MainForm'daki "Yazdır" butonuyla) kullanıcı her seferinde bu
            // ayarları elle seçmek zorunda kalmaz.
            sheet.FitToPage = true;
            sheet.PrintSetup.Landscape = true;
            sheet.PrintSetup.FitWidth = 1;
            sheet.PrintSetup.FitHeight = 1;

            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);
            ApplyThinBorder(headerStyle);

            var dataStyle = workbook.CreateCellStyle();
            ApplyThinBorder(dataStyle);

            var headerRow = sheet.CreateRow(0);
            for (int c = 0; c < Headers.Length; c++)
            {
                var cell = headerRow.CreateCell(c);
                cell.SetCellValue(Headers[c]);
                cell.CellStyle = headerStyle;
                sheet.SetColumnWidth(c, ColumnWidths[c] * 256);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var row = sheet.CreateRow(i + 1);
                ICell Cell(int c)
                {
                    var cell = row.CreateCell(c);
                    cell.CellStyle = dataStyle;
                    return cell;
                }
                Cell(0).SetCellValue(r.ProductRoad);
                Cell(1).SetCellValue(r.ProductCode);
                Cell(2).SetCellValue(r.Quality);
                Cell(3).SetCellValue(r.Pattern);
                Cell(4).SetCellValue(r.Color);
                Cell(5).SetCellValue(r.Size);
                Cell(6).SetCellValue(r.Edge);
                Cell(7).SetCellValue(r.Serial);
                Cell(8).SetCellValue(r.Ean);
                Cell(ImageColumnIndex); // resmin çerçevesi: hücre boş ama sınırlı kalsın
            }

            if (summary != null)
            {
                // Elle üretim emrinde toplamlar en alta değil, resmin sağında (bir
                // boş kolon arayla) geniş bir "Bilgi" kolonunda, resimle aynı
                // hizada (üst satırlardan itibaren) gösterilir.
                WriteSummary(sheet, headerRow, summary, rows.Count, headerStyle);
            }
            else
            {
                // En alta toplam adet satırı. Etiket "Toplam" ile başladığından
                // içe aktarım tarafında veri satırı sanılmaz, atlanır.
                var totalRow = sheet.CreateRow(rows.Count + 1);
                var totalCell = totalRow.CreateCell(1);
                totalCell.SetCellValue($"Toplam Adet : {rows.Count}");
                totalCell.CellStyle = headerStyle;
            }

            EmbedImages(workbook, sheet, rows);

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            workbook.Write(fs);
        }

        private static void ApplyThinBorder(ICellStyle style)
        {
            style.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            style.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            style.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            style.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
        }

        private static void WriteSummary(NPOI.SS.UserModel.ISheet sheet, NPOI.SS.UserModel.IRow headerRow,
            ManualOrderSummary summary, int rowCount, ICellStyle boldStyle)
        {
            sheet.SetColumnWidth(SummaryColumnIndex, SummaryColumnWidth * 256);

            var titleCell = headerRow.CreateCell(SummaryColumnIndex);
            titleCell.SetCellValue("Bilgi");
            titleCell.CellStyle = boldStyle;

            void WriteLine(int rowIndex, string text)
            {
                var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
                var cell = row.CreateCell(SummaryColumnIndex);
                cell.SetCellValue(text);
                cell.CellStyle = boldStyle;
            }

            // 1. satırdan başlar: resim de aynı satırdan (bkz. EmbedOnePicture) başladığından hizalı durur.
            WriteLine(1, $"Tarih: {summary.Date:dd.MM.yyyy}");
            WriteLine(2, $"Program No: {summary.ProgramNo}");
            WriteLine(3, $"Toplam Adet : {rowCount}");
            WriteLine(4, $"Toplam m²: {summary.TotalM2:0.##}");
        }

        /// <summary>
        /// Aynı Desen'e ait satırlar ConvertManual'da art arda üretildiğinden
        /// (bkz. ConversionService), aynı ImagePath'e sahip ardışık satır
        /// bloğu başına bir kez resim gömülür - her parça satırına ayrı ayrı
        /// değil, tekrar tekrar aynı resmi eklemek gereksiz büyütür.
        /// </summary>
        private static void EmbedImages(XSSFWorkbook workbook, NPOI.SS.UserModel.ISheet sheet, IReadOnlyList<ProductEntryRow> rows)
        {
            var drawing = sheet.CreateDrawingPatriarch();
            string? currentPath = null;
            int groupStartIndex = 0;

            for (int i = 0; i <= rows.Count; i++)
            {
                var path = i < rows.Count ? rows[i].ImagePath : null;
                if (path == currentPath) continue;

                if (!string.IsNullOrWhiteSpace(currentPath) && File.Exists(currentPath))
                    EmbedOnePicture(workbook, drawing, currentPath, groupStartIndex + 1); // +1: 0. satır başlık

                currentPath = path;
                groupStartIndex = i;
            }
        }

        private static void EmbedOnePicture(XSSFWorkbook workbook, NPOI.SS.UserModel.IDrawing<NPOI.SS.UserModel.IShape> drawing, string imagePath, int rowIndex)
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(imagePath);
            }
            catch (IOException)
            {
                return; // dosya artık okunamıyorsa (taşınmış/silinmiş) resmi atla, dönüşümü bozma
            }

            var pictureIndex = workbook.AddPicture(bytes, GetPictureType(imagePath));
            var anchor = workbook.GetCreationHelper().CreateClientAnchor();
            anchor.Col1 = ImageColumnIndex;
            anchor.Row1 = rowIndex;
            anchor.Col2 = ImageColumnIndex + 1;
            anchor.Row2 = rowIndex + ImageRowSpan;
            drawing.CreatePicture(anchor, pictureIndex);
        }

        private static PictureType GetPictureType(string path) =>
            Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => PictureType.PNG,
                ".jpg" or ".jpeg" => PictureType.JPEG,
                ".gif" => PictureType.GIF,
                ".tif" or ".tiff" => PictureType.TIFF,
                _ => PictureType.BMP,
            };
    }
}
