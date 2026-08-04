using TurcaExce.Config;

namespace TurcaExce.Services
{
    /// <summary>
    /// Kalıcı ebat kayıt defteri. Desen dosya adındaki kısaltılmış ebat kodu
    /// (örn. 4X25) normal WxH biçiminde değildir (bkz. PatternParser); gerçek
    /// ölçü kullanıcıya bir kez sorulup sizes.txt dosyasına "KOD[TAB]EBAT"
    /// olarak kaydedilir, sonraki çalıştırmalarda buradan okunur.
    /// </summary>
    public class SizeRegistry
    {
        public static string FilePath =>
            Path.Combine(AppPaths.DataDirectory, "sizes.txt");

        public Dictionary<string, string> Map { get; } = new(StringComparer.OrdinalIgnoreCase);

        private SizeRegistry() { }

        public static SizeRegistry Load()
        {
            var registry = new SizeRegistry();

            if (File.Exists(FilePath))
            {
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var idx = line.IndexOf('\t');
                    if (idx < 0) continue;

                    var code = line[..idx].Trim().ToUpperInvariant();
                    var size = line[(idx + 1)..].Trim();
                    if (size.Length == 0) continue;

                    registry.Map[code] = size;
                }
            }

            return registry;
        }

        /// <summary>Yeni öğrenilen ebatı tabloya ekler ve dosyayı hemen günceller.</summary>
        public void Add(string code, string size)
        {
            Map[code.Trim().ToUpperInvariant()] = size.Trim();
            Save();
        }

        public void Save() =>
            File.WriteAllLines(FilePath,
                Map.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                   .Select(kv => $"{kv.Key}\t{kv.Value}"));
    }
}
