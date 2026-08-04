namespace TurcaExce.Services
{
    /// <summary>Kenar seçenekleri, kodda sabit gömülü (elle üretim emri girişinde kullanılır).</summary>
    public static class EdgeOptions
    {
        public static readonly (string Name, string Letter)[] All =
        [
            ("Deri + Overlok", "DO"),
            ("Deri + Saçak", "DS"),
            ("Deri + Yapıştırma", "DY"),
            ("Overlok", "O"),
            ("Rulo", "R"),
            ("Saçak", "S"),
            ("Yapıştırma", "Y"),
        ];
    }
}
