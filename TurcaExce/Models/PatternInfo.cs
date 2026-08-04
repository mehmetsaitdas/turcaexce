namespace TurcaExce.Models
{
    /// <summary>Desen dosya adının (65A7101AGRI200X290.ep) çözümlenmiş hali.</summary>
    public class PatternInfo
    {
        public string FileName { get; set; } = "";

        /// <summary>Örn. 65A — Kalite kolonuna yazılır.</summary>
        public string Prefix { get; set; } = "";

        /// <summary>Örn. 7101 — Desen kolonuna yazılır.</summary>
        public string PatternNo { get; set; } = "";

        /// <summary>
        /// Numara ile ebat arasındaki harflerin tamamı: renk kodu veya boş.
        /// Birleşik olabilir (KMKBLU = Kemik Blue); baştaki A/K, Açık/Koyu
        /// anlamına gelebilir (AGRI, KGRI).
        /// </summary>
        public string ColorCode { get; set; } = "";

        /// <summary>Örn. 200X290</summary>
        public string Size { get; set; } = "";
    }
}
