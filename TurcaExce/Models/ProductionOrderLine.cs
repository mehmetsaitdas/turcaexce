namespace TurcaExce.Models
{
    /// <summary>123.xls (üretim emri) içindeki tek bir desen satırı.</summary>
    public class ProductionOrderLine
    {
        /// <summary>YOL NO bloğu.</summary>
        public int PathNo { get; set; }

        /// <summary>Blok içindeki NO değeri.</summary>
        public int ItemNo { get; set; }

        /// <summary>Örn. 65A7101AGRI200X290.ep</summary>
        public string PatternFileName { get; set; } = "";

        /// <summary>S.AD sütunu.</summary>
        public int SCount { get; set; }

        /// <summary>D.ADET sütunu — üretilecek adet; çıktıda satır sayısını belirler.</summary>
        public int Quantity { get; set; } = 1;

        /// <summary>Boy (Metre) sütunu.</summary>
        public double LengthMeters { get; set; }

        /// <summary>Atkı sütunu.</summary>
        public double WeftCount { get; set; }

        /// <summary>Yol Uzunluğu sütunu.</summary>
        public double PathLength { get; set; }
    }
}
