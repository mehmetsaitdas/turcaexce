namespace TurcaExce.Models
{
    /// <summary>Excel_Girisi.xlsx formatındaki tek bir çıktı satırı.</summary>
    public class ProductEntryRow
    {
        public int ProductRoad { get; set; } //Yol
        public string ProductCode { get; set; } = "";
        public string Quality { get; set; } = "";
        public string Pattern { get; set; } = "";
        public string Color { get; set; } = "";
        public string Size { get; set; } = "";
        public string Edge { get; set; } = "";
        public string Serial { get; set; } = "";
        public string Ean { get; set; } = "";

        /// <summary>Bu satırın geldiği Desen'e ait halı resminin tam dosya yolu (yoksa null).</summary>
        public string? ImagePath { get; set; }

        /// <summary>
        /// Kalite kolonundaki adın kaynaklandığı ham kod (örn. 72A). Kalite
        /// Revize akışında satırları koda göre gruplamak için kullanılır;
        /// çıktı Excel'ine yazılmaz (bkz. ExcelWriter, gridPreview'da gizli -
        /// bkz. MainForm.ApplyConversionResult). Sona eklendi ki gridPreview'un
        /// otomatik oluşturduğu kolonların index'i (ExcelWriter.Headers ile
        /// eşleşen 0-9 arası) kaymasın.
        /// </summary>
        public string QualityCode { get; set; } = "";

        /// <summary>
        /// ProductCode'un Kalite Adı segmentinden sonraki kısmı (Desen_Renk_
        /// Ebat+Kenar, zaten ASCII/büyük harf). Kalite Revize bir kodu yeniden
        /// adlandırınca ProductCode'u ("KaliteAdı_..." biçiminde) bu ekten
        /// yeniden kurmak için kullanılır - bkz. MainForm.btnQualityRev_Click.
        /// Çıktı Excel'ine yazılmaz, gridPreview'da gizli.
        /// </summary>
        public string ProductCodeSuffix { get; set; } = "";
    }
}
