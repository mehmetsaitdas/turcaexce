using TurcaExce.Config;

namespace TurcaExce.Services
{
    /// <summary>
    /// Kalıcı EAN kayıt defteri. Uygulama klasöründeki ean_registry.txt
    /// dosyasında "DESEN[TAB]EAN" satırları halinde tutulur. Aynı ürün kalemi
    /// (desen dosya adı) daha önceki bir çalıştırmada EAN aldıysa yenisi
    /// üretilmez, kayıttaki EAN aynen kullanılır.
    /// </summary>
    public class EanRegistry
    {
        public static string FilePath =>
            Path.Combine(AppPaths.DataDirectory, "ean_registry.txt");

        private readonly Dictionary<string, string> _eanByPattern = new(StringComparer.OrdinalIgnoreCase);
        private long _nextBase;

        private EanRegistry() { }

        public static EanRegistry Load(long startingEan)
        {
            MigrateToV2IfNeeded();

            var registry = new EanRegistry { _nextBase = startingEan };

            if (File.Exists(FilePath))
            {
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var parts = line.Split('\t');
                    if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0])) continue;

                    var storedEan = parts[1].Trim();
                    registry._eanByPattern[parts[0].Trim()] = storedEan;

                    // Yeni EAN'lar, kayıtlı en büyük taban numaranın üzerinden devam
                    // etsin ki eski kayıtlarla çakışma olmasın. Kayıtlı deger dogru
                    // formatta EAN-13 (13 hane) ise kontrol hanesi haric ilk 12 hane
                    // taban sayidir; eski/bozuk (13 haneden farkli) kayitlarda ise
                    // deger dogrudan taban sayi olarak alinir.
                    var baseDigits = storedEan.Length == 13 ? storedEan[..12] : storedEan;
                    if (long.TryParse(baseDigits, out var basePart) && basePart >= registry._nextBase)
                        registry._nextBase = basePart + 1;
                }
            }

            return registry;
        }

        /// <summary>Kayıtlı EAN-13'ü döner; yoksa 12 haneli tabana GS1/EAN-13 kontrol
        /// hanesi eklenerek yeni, gecerli 13 haneli EAN üretilip kaydedilir.</summary>
        public string GetOrCreate(string patternKey)
        {
            patternKey = patternKey.Trim().ToUpperInvariant();
            if (!_eanByPattern.TryGetValue(patternKey, out var ean))
            {
                var base12 = (_nextBase++).ToString().PadLeft(12, '0');
                ean = base12 + ComputeCheckDigit(base12);
                _eanByPattern[patternKey] = ean;
            }
            return ean;
        }

        /// <summary>Standart EAN-13 (GS1) mod-10 kontrol hanesi: soldan 1. hanenin
        /// agirligi 1, 2. hanenin agirligi 3, sirayla devam eder.</summary>
        private static char ComputeCheckDigit(string base12)
        {
            int sum = 0;
            for (int i = 0; i < base12.Length; i++)
            {
                int digit = base12[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }
            int check = (10 - (sum % 10)) % 10;
            return (char)('0' + check);
        }

        public void Save() =>
            File.WriteAllLines(FilePath,
                _eanByPattern.OrderBy(kv => kv.Value, StringComparer.Ordinal)
                             .Select(kv => $"{kv.Key}\t{kv.Value}"));

        /// <summary>
        /// 13 haneli, dogru kontrol haneli EAN uretimine gecildi (eskiden kontrol
        /// hanesi ve 12. hane olmadan, dogrudan 11 haneli sayac degeri EAN olarak
        /// kaydediliyordu). O eski kayitlar musterilere zaten gitmis, gecerli
        /// entegrasyonlar olabilir; bu yuzden sessizce/otomatik migrate edilmez,
        /// sadece eski kayit defteri bir kerelik kenara alinir ve sayac
        /// StartingEan'dan sifirdan baslar. Her istemcide (makinede) BIR KEZ
        /// calisir - bayrak dosyasi sayesinde bir daha asla tekrarlanmaz, aksi
        /// halde her acilista onceden verilmis EAN'lar yeniden uretilirdi.
        /// </summary>
        private static void MigrateToV2IfNeeded()
        {
            var flagPath = Path.Combine(AppPaths.DataDirectory, "ean_registry_v2.flag");
            if (File.Exists(flagPath)) return;

            if (File.Exists(FilePath))
            {
                var backupPath = Path.Combine(AppPaths.DataDirectory,
                    $"ean_registry_eski_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                File.Move(FilePath, backupPath);
            }

            File.WriteAllText(flagPath, $"EAN-13 formatina (13 hane, kontrol haneli) gecis: {DateTime.Now:O}");
        }
    }
}
