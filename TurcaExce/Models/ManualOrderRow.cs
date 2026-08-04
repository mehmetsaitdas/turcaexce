namespace TurcaExce.Models
{
    /// <summary>
    /// Üretim emri Excel'i olmayan müşteriler için elle girilen tek bir
    /// desen+renk satırı. Ebat sütunu başına girilen adet Quantities'te
    /// tutulur (anahtar: "300X400" gibi normalize edilmiş ebat metni).
    /// </summary>
    public class ManualOrderRow
    {
        public int Road { get; set; } = 1;
        public string Pattern { get; set; } = "";
        public string Color { get; set; } = "";
        public string Edge { get; set; } = "";
        public Dictionary<string, int> Quantities { get; } = new();

        /// <summary>Bu Desen/Renk satırı için seçilen halı resminin tam dosya yolu (yoksa null).</summary>
        public string? ImagePath { get; set; }
    }
}
