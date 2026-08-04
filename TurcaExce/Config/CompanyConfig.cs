using TurcaExce.Services;

namespace TurcaExce.Config
{
    /// <summary>
    /// %AppData%\TurcaExce\config.ini içindeki [main] name= değeri.
    /// Birden fazla firma bu programı kullanırsa, çıktı seri numaralarının
    /// (Ser) firmalar arasında çakışmaması için firma adının ilk üç
    /// sessiz harfi seri numarasının başına eklenir (bkz. SerialPrefix).
    /// </summary>
    public class CompanyConfig
    {
        public string Name { get; set; } = "";

        public static string FilePath => Path.Combine(AppPaths.DataDirectory, "config.ini");

        public static CompanyConfig Load()
        {
            var config = new CompanyConfig();
            if (!File.Exists(FilePath)) return config;

            foreach (var line in File.ReadAllLines(FilePath))
            {
                var trimmed = line.Trim();
                var eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;

                var key = trimmed[..eq].Trim();
                var value = trimmed[(eq + 1)..].Trim();
                if (key.Equals("name", StringComparison.OrdinalIgnoreCase))
                    config.Name = value;
            }
            return config;
        }

        public void Save() =>
            File.WriteAllLines(FilePath,
            [
                "[main]",
                $"name={Name}",
            ]);

        /// <summary>
        /// Seri numarası öneki: firma adındaki ilk 3 sessiz harf, Türkçe
        /// karakterler sadeleştirilip büyük harfe çevrilir.
        /// Örnek: "Yalçın Kardeşler" -> "YLC", "Faraj Halı" -> "FRJ".
        /// Sessiz harf 3'ten azsa bulunanla yetinilir; hiç harf yoksa boş döner.
        /// </summary>
        public string SerialPrefix()
        {
            const string vowels = "AEIİOÖUÜaeıioöuü";

            var consonants = Name
                .Where(char.IsLetter)
                .Where(c => !vowels.Contains(c))
                .Select(TurkishText.ToAsciiUpper)
                .Take(3)
                .ToArray();

            if (consonants.Length > 0)
                return new string(consonants);

            // Sessiz harf bulunamadıysa (örn. tamamı sesli), ilk 3 harfi kullan.
            return new string(Name.Where(char.IsLetter).Select(TurkishText.ToAsciiUpper).Take(3).ToArray());
        }
    }
}
