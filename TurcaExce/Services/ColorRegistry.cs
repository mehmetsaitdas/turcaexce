using TurcaExce.Config;

namespace TurcaExce.Services
{
    /// <summary>
    /// Kalıcı renk kayıt defteri. Uygulama klasöründeki colors.txt dosyasında
    /// "KOD[TAB]Renk Adı" satırları halinde tutulur. İlk açılışta (dosya yokken)
    /// koda gömülü statik renk tablosu bu dosyaya yazılır; sonraki açılışlarda
    /// renkler yalnızca bu dosyadan okunur. Dönüşüm sırasında bilinmeyen bir
    /// kod için kullanıcıdan alınan ad da Add ile buraya eklenip kaydedilir.
    /// </summary>
    public class ColorRegistry
    {
        public static string FilePath =>
            Path.Combine(AppPaths.DataDirectory, "colors.txt");

        /// <summary>Renk kodu -> renk adı. Boş anahtar, renk kodu olmayan desenler içindir.</summary>
        public Dictionary<string, string> Map { get; } = new(StringComparer.OrdinalIgnoreCase);

        private ColorRegistry() { }

        /// <summary>
        /// Dosya varsa oradan okur; yoksa verilen statik tabloyu dosyaya yazıp
        /// onunla başlar.
        /// </summary>
        public static ColorRegistry Load(IReadOnlyDictionary<string, string> defaults)
        {
            var registry = new ColorRegistry();

            if (File.Exists(FilePath))
            {
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var idx = line.IndexOf('\t');
                    if (idx < 0) continue;

                    var code = line[..idx].Trim().ToUpperInvariant();
                    var name = line[(idx + 1)..].Trim();
                    if (name.Length == 0) continue;

                    registry.Map[code] = name;
                }

                // Renk kodu olmayan desenler için boş anahtar her zaman bulunsun.
                registry.Map.TryAdd("", defaults.GetValueOrDefault("", "Standart"));
            }
            else
            {
                foreach (var kv in defaults)
                    registry.Map[kv.Key] = kv.Value;
                registry.Save();
            }

            return registry;
        }

        /// <summary>Yeni öğrenilen rengi tabloya ekler ve dosyayı hemen günceller.</summary>
        public void Add(string code, string name)
        {
            Map[code.Trim().ToUpperInvariant()] = name.Trim();
            Save();
        }

        public void Save() =>
            File.WriteAllLines(FilePath,
                Map.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                   .Select(kv => $"{kv.Key}\t{kv.Value}"));
    }
}
