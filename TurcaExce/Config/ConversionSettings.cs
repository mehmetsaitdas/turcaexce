using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace TurcaExce.Config
{
    /// <summary>
    /// Dönüşümde kullanılan tüm eşleşme tabloları ve sayaç başlangıçları.
    /// Uygulama klasöründeki conversion_settings.json dosyasından okunur;
    /// dosya yoksa varsayılanlarla oluşturulur. Eşleşmeler değiştiğinde JSON
    /// dosyasını düzenlemek yeterlidir, kod değişikliği gerekmez.
    /// </summary>
    public class ConversionSettings
    {
        // Kalite = dosya adındaki prefix'in kendisi (65A). Bir prefix'in başka
        // bir kalite adına çevrilmesi gerekirse buraya eklenir; tabloda
        // olmayan prefix aynen kullanılır.
        public Dictionary<string, string> QualityMap { get; set; } = new();

        // Renk kodu -> renk adı. Kod tabloda yoksa baştaki A/K harfi
        // Açık/Koyu öneki olarak denenir (AGRI -> Açık Gri, KBEJ -> Koyu Bej);
        // özel bir karşılık gerekiyorsa (AGRI gibi) buraya açıkça yazılabilir,
        // açık kayıt her zaman önceliklidir. Boş anahtar, renk kodu olmayan
        // desenler içindir. Liste tahmini kısaltmalarla dolduruldu; müşteriden
        // yeni kod geldikçe eklenir.
        public Dictionary<string, string> ColorMap { get; set; } = new()
        {
            [""] = "Standart",
            // Bu dosyada görülenler (KMK = Kemik; KMKBLU gibi birleşik kodlar
            // parçalanarak "Kemik Blue" şeklinde ada çevrilir)
            ["GRI"] = "Gri",
            ["GOLD"] = "Gold",
            ["BLU"] = "Blue",
            ["KMK"] = "Kemik",
            // Muhtemel Türkçe kısaltmalar
            ["BEJ"] = "Bej",
            ["KREM"] = "Krem",
            ["KRM"] = "Krem",
            ["VIZON"] = "Vizon",
            ["VZN"] = "Vizon",
            ["ANT"] = "Antrasit",
            ["ANTRASIT"] = "Antrasit",
            ["FUME"] = "Füme",
            ["EKRU"] = "Ekru",
            ["MAVI"] = "Mavi",
            ["LAC"] = "Lacivert",
            ["LACI"] = "Lacivert",
            ["LACIVERT"] = "Lacivert",
            ["SIYAH"] = "Siyah",
            ["SYH"] = "Siyah",
            ["BEYAZ"] = "Beyaz",
            ["BYZ"] = "Beyaz",
            ["KAHVE"] = "Kahve",
            ["KHV"] = "Kahve",
            ["TABA"] = "Taba",
            ["TERRA"] = "Terra",
            ["PUDRA"] = "Pudra",
            ["PDR"] = "Pudra",
            ["LILA"] = "Lila",
            ["MOR"] = "Mor",
            ["MURDUM"] = "Mürdüm",
            ["YESIL"] = "Yeşil",
            ["YSL"] = "Yeşil",
            ["HAKI"] = "Haki",
            ["KIRMIZI"] = "Kırmızı",
            ["KRMZ"] = "Kırmızı",
            ["BORDO"] = "Bordo",
            ["TURKUAZ"] = "Turkuaz",
            ["TRK"] = "Turkuaz",
            ["HARDAL"] = "Hardal",
            ["SOMON"] = "Somon",
            ["SARI"] = "Sarı",
            ["PEMBE"] = "Pembe",
            ["GUMUS"] = "Gümüş",
            ["BAKIR"] = "Bakır",
            ["MINT"] = "Mint",
            // Muhtemel İngilizce kısaltmalar
            ["BLUE"] = "Mavi",
            ["NAVY"] = "Lacivert",
            ["GREY"] = "Gri",
            ["GRAY"] = "Gri",
            ["CREAM"] = "Krem",
            ["BROWN"] = "Kahve",
            ["BLACK"] = "Siyah",
            ["WHITE"] = "Beyaz",
            ["RED"] = "Kırmızı",
            ["GREEN"] = "Yeşil",
            ["PINK"] = "Pembe",
            ["ROSE"] = "Rose",
            ["SILVER"] = "Gümüş",
            ["COPPER"] = "Bakır",
            ["BEIGE"] = "Bej",
            ["MUSTARD"] = "Hardal",
            ["SALMON"] = "Somon",
            ["YELLOW"] = "Sarı",
            ["PURPLE"] = "Mor",
            ["TERRACOTTA"] = "Terra",
        };

        /// <summary>Tabloda olmayan kodun başındaki A/K harfine verilecek önekler.</summary>
        public string LightPrefixName { get; set; } = "Açık";
        public string DarkPrefixName { get; set; } = "Koyu";

        // UrunKodu içindeki renk segmenti (örn. 65A_7101_AGRI_200X290S).
        // Tabloda olmayan kod aynen kullanılır; boş kod STD olur.
        public Dictionary<string, string> ProductCodeColorSegmentMap { get; set; } = new()
        {
            [""] = "STD",
        };

        /// <summary>Kenar tüm ürünlerde aynıdır.</summary>
        public string Edge { get; set; } = "Saçak";

        // UrunKodu sonundaki kenar harfi (200X290 + harf).
        public Dictionary<string, string> EdgeLetterMap { get; set; } = new()
        {
            ["Overlok"] = "O",
            ["Saçak"] = "S",
            ["Yapıştırma"] = "Y",
            ["Deri + Yapıştırma"] = "D",
        };

        /// <summary>Ebat kolonunda 200x290 sonuna eklenen ek (hedef örnekte hep D).</summary>
        public string SizeSuffix { get; set; } = "D";

        /// <summary>
        /// Halılar alt-üst dokunduğu için D.ADET'teki her 1, gerçekte bu kadar
        /// adet ürün demektir (D.ADET 1 -> 2 kayıt).
        /// </summary>
        public int QuantityMultiplier { get; set; } = 2;

        public long StartingSerial { get; set; } = 1000021;
        public long StartingEan { get; set; } = 98705405174;

        /// <summary>
        /// E-posta gönderim ayarları. Alıcı adresi arayüzden bir kez yazılınca
        /// buraya kaydedilir; gönderim Outlook girişiyle alınan token üzerinden yapılır.
        /// </summary>
        public EmailSettings Email { get; set; } = new();

        [JsonIgnore]
        public static string FilePath =>
            Path.Combine(AppPaths.DataDirectory, "conversion_settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        };

        public static ConversionSettings Load()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<ConversionSettings>(json, JsonOptions) ?? new ConversionSettings();

                // Eski (bin klasöründen taşınmış) bir dosyada Client ID boş
                // kalmışsa koda gömülü varsayılanla doldur ve kaydet.
                if (string.IsNullOrWhiteSpace(loaded.Email.AzureAdClientId))
                {
                    loaded.Email.AzureAdClientId = new EmailSettings().AzureAdClientId;
                    loaded.Save();
                }

                return loaded;
            }

            var settings = new ConversionSettings();
            settings.Save();
            return settings;
        }

        public void Save() =>
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
