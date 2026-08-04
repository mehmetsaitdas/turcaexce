namespace TurcaExce.Models
{
    /// <summary>
    /// Elle üretim emri Excel'inin liste başına eklenen özet bilgisi
    /// (Program No, Tarih, Toplam m²) — bkz. ExcelWriter.Write.
    /// </summary>
    public class ManualOrderSummary
    {
        public string ProgramNo { get; set; } = "";
        public DateTime Date { get; set; }
        public double TotalM2 { get; set; }
    }
}
