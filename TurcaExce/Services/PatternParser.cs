using System.Text.RegularExpressions;
using TurcaExce.Models;

namespace TurcaExce.Services
{
    /// <summary>
    /// Desen dosya adını parçalar:
    /// [Prefix: 65A][DesenNo: 3-4 hane][Renk kodu: harfler][Ebat: 200X290].ep
    /// Numara ile ebat arasındaki harflerin tamamı renk kodudur; KMK dahil
    /// (KMK = Kemik). Birleşik kodların (KMKBLU -> Kemik Blue) ada çevrimi
    /// ConversionService'tedir.
    /// </summary>
    public static partial class PatternParser
    {
        // Ebat normalde 2-3 haneli x 2-3 haneli (200X290, 80X150); bazı desen
        // adlarında kısaltılmış görülür (4X25 = 400x2500 demek ama harf/hane
        // sayısından anlaşılmaz). 1-4 hane aralığı geniş tutulur ki en azından
        // dosya adı çözümlensin; kısaltılmış olup olmadığına ConversionService
        // karar verir (bkz. ResolveSize).
        //
        // Ebattan sonra tek bir harf gelebilir (160X230O, 160X160D gibi -
        // kaynaktaki kenar/kesim isaretidir); bu harf su an kullanilmiyor,
        // yalnizca yok sayilir ki dosya adi "cozumlenemedi" diye atlanmasin -
        // satirin kenari her zamanki gibi ConversionSettings.Edge'den gelir.
        //
        // Desen no'sundan once tek bir harf gelebilir (48AS258 -> prefix 48A,
        // desen no S258). Desen no'sundan sonra alt cizgiyle ayrilmis bir harf
        // de gelebilir (257B_VZN -> desen no 257B, renk VZN); bu harf rengin
        // parcasi degildir - renk kaydinda (colors.txt) sadece "VZN" bulunur,
        // "B" tondur ve desen no'suna eklenir.
        [GeneratedRegex(@"^(?<prefix>\d{2}[A-Z])(?<no>[A-Z]?\d{3,4})(?:(?<letter>[A-Z])_(?<color>[A-Z]*)|_?(?<color>[A-Z]*))(?<size>\d{1,4}X\d{1,4})[A-Z]?\.EP$",
            RegexOptions.IgnoreCase)]
        private static partial Regex PatternRegex();

        /// <summary>Çözümlenemezse null döner.</summary>
        public static PatternInfo? Parse(string fileName)
        {
            var m = PatternRegex().Match(fileName.Trim());
            if (!m.Success) return null;

            var letter = m.Groups["letter"].Value;

            return new PatternInfo
            {
                FileName = fileName.Trim(),
                Prefix = m.Groups["prefix"].Value.ToUpperInvariant(),
                PatternNo = (m.Groups["no"].Value + letter).ToUpperInvariant(),
                ColorCode = m.Groups["color"].Value.ToUpperInvariant(),
                Size = m.Groups["size"].Value.ToUpperInvariant(),
            };
        }
    }
}
