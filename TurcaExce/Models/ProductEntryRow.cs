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
    }
}
