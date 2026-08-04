using TurcaExce.Config;
using TurcaExce.Models;

namespace TurcaExce.Services
{
    public class ConversionResult
    {
        public List<ProductEntryRow> Rows { get; } = [];
        public List<string> Warnings { get; } = [];
    }

    /// <summary>
    /// Üretim emri satırlarını Excel_Girisi formatına çevirir.
    /// Çıktı kaynak dosyadaki sırayı izler: önce yol 1'in kalemleri sırayla
    /// biter, sonra yol 2'ye geçilir (yollar karıştırılmaz). Her kaynak satır
    /// D.ADET x alt-üst çarpanı kadar çıktı satırı üretir; Ser bu sıraya göre
    /// sürekli artar. EAN ise DESEN (dosya adı) başına birdir ve kalıcı
    /// kayıt defterinden (ean_registry.txt) gelir: aynı ürün kalemi önceki
    /// çalıştırmalarda EAN aldıysa aynı numara tekrar kullanılır.
    /// </summary>
    public class ConversionService(
        ConversionSettings settings,
        EanRegistry eanRegistry,
        ColorRegistry colorRegistry,
        SizeRegistry sizeRegistry,
        Func<string, string?>? askUnknownColor = null,
        Func<string, string?>? askUnknownSize = null)
    {
        // Aynı bilinmeyen kod bir çalıştırmada yalnızca bir kez sorulur;
        // kullanıcı boş geçtiyse tekrar sorulmaz.
        private readonly HashSet<string> _declinedCodes = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _declinedSizes = new(StringComparer.OrdinalIgnoreCase);

        public ConversionResult Convert(IEnumerable<ProductionOrderLine> sourceLines)
        {
            var result = new ConversionResult();
            long serial = settings.StartingSerial;
            // Firma bazlı önek: aynı seri numarasının farklı firmalarda
            // çakışmasını önler (bkz. Config.CompanyConfig.SerialPrefix).
            var serialPrefix = CompanyConfig.Load().SerialPrefix();

            foreach (var source in sourceLines)
            {
                var p = PatternParser.Parse(source.PatternFileName);
                if (p == null)
                {
                    result.Warnings.Add(
                        $"Yol {source.PathNo} / No {source.ItemNo}: \"{source.PatternFileName}\" çözümlenemedi, satır atlandı.");
                    continue;
                }

                // Kalite = prefix'in kendisi; tabloda özel bir karşılığı varsa o kullanılır.
                var quality = settings.QualityMap.GetValueOrDefault(p.Prefix, p.Prefix);

                var color = ResolveColor(p.ColorCode, out var colorResolved);
                if (!colorResolved)
                    result.Warnings.Add(
                        $"Yol {source.PathNo} / No {source.ItemNo}: \"{p.ColorCode}\" renk kodu çözümlenemedi, kod aynen kullanıldı. Gerekirse colors.txt dosyasına ekleyin.");

                var realSize = ResolveSize(p.Size, out var sizeResolved);
                if (!sizeResolved)
                    result.Warnings.Add(
                        $"Yol {source.PathNo} / No {source.ItemNo}: \"{p.Size}\" ebat kodu tanınmadı, kod aynen kullanıldı. Gerekirse sizes.txt dosyasına ekleyin.");

                var pattern = p.PatternNo;
                var edge = settings.Edge;
                var edgeLetter = settings.EdgeLetterMap.GetValueOrDefault(edge,
                    edge.Length > 0 ? edge[..1].ToUpperInvariant() : "");
                var colorSegment = settings.ProductCodeColorSegmentMap.GetValueOrDefault(p.ColorCode, p.ColorCode);
                var size = realSize.Replace("X", "x") + settings.SizeSuffix;
                // UrunKodu tamamen buyuk harf/ASCII olmali (kalite/kaynak dosya adi/ayar
                // tablolari kucuk harf ya da Turkce karakter icerebilir - bkz. ConvertManual).
                var productCode = TurkishText.ToAsciiUpper($"{quality}_{pattern}_{colorSegment}_{realSize}{edgeLetter}");

                var ean = eanRegistry.GetOrCreate(p.FileName);

                // Alt-üst dokuma: D.ADET'teki her 1, çarpan kadar gerçek ürün eder.
                var pieceCount = source.Quantity * Math.Max(1, settings.QuantityMultiplier);

                // Ser: her adet (çıktı satırı) için ayrı, sürekli artan numara.
                for (int i = 0; i < pieceCount; i++)
                {
                    result.Rows.Add(new ProductEntryRow
                    {
                        ProductRoad = source.PathNo,
                        ProductCode = productCode,
                        Quality = quality,
                        Pattern = pattern,
                        Color = color,
                        Size = size,
                        Edge = edge,
                        Serial = $"{serialPrefix}{serial++}",
                        Ean = ean,
                    });
                }
            }

            // Bir sonraki çalıştırma/liste bıraktığı yerden devam etsin diye
            // son kullanılan seri numarası kalıcı ayarlara geri yazılır.
            settings.StartingSerial = serial;
            settings.Save();

            return result;
        }

        /// <summary>
        /// Renk kodunu ada çevirir. Kod birleşik olabilir (KMKBLU = Kemik Blue,
        /// KMKAGRI = Kemik Açık Gri); soldan başlayarak en uzun eşleşen parça
        /// bulunur ve adları birleştirilir. Her parça için önce tabloya bakılır
        /// (açık kayıt her zaman kazanır); yoksa baştaki A/K harfi Açık/Koyu
        /// öneki olarak denenir (AGRI -> Açık Gri, KBEJ -> Koyu Bej).
        /// Hiçbir parça çözülemezse (varsa) askUnknownColor ile kullanıcıya
        /// sorulur; verilen ad colors.txt'ye kaydedilip kullanılır. Kullanıcı
        /// boş geçerse kod aynen döner ve resolved false olur.
        /// </summary>
        private string ResolveColor(string colorCode, out bool resolved)
        {
            resolved = true;

            if (colorRegistry.Map.TryGetValue(colorCode, out var direct))
                return direct;

            var names = new List<string>();
            int i = 0;
            while (i < colorCode.Length)
            {
                string? matchName = null;
                int matchLength = 0;

                // Soldan en uzun eşleşme: önce tablodaki kod, sonra A/K + kod.
                for (int len = colorCode.Length - i; len >= 1 && matchName == null; len--)
                {
                    var part = colorCode.Substring(i, len);
                    if (colorRegistry.Map.TryGetValue(part, out var name))
                    {
                        matchName = name;
                        matchLength = len;
                    }
                    else if (len > 1 && (part[0] == 'A' || part[0] == 'K') &&
                             colorRegistry.Map.TryGetValue(part[1..], out var baseName))
                    {
                        var prefix = part[0] == 'A' ? settings.LightPrefixName : settings.DarkPrefixName;
                        matchName = $"{prefix} {baseName}";
                        matchLength = len;
                    }
                }

                if (matchName == null)
                {
                    // Tanınmayan kod: kullanıcıya sor, cevabı colors.txt'ye
                    // kaydet ve o adı kullan. Boş geçilirse kod aynen kalır.
                    if (askUnknownColor != null && !_declinedCodes.Contains(colorCode))
                    {
                        var answer = askUnknownColor(colorCode);
                        if (!string.IsNullOrWhiteSpace(answer))
                        {
                            colorRegistry.Add(colorCode, answer);
                            return answer.Trim();
                        }
                        _declinedCodes.Add(colorCode);
                    }

                    resolved = false;
                    return colorCode;
                }

                names.Add(matchName);
                i += matchLength;
            }

            return string.Join(" ", names);
        }

        /// <summary>
        /// Ebat kodunu gerçek ölçüye çevirir. Normal biçim (160X230, 80X150
        /// gibi her iki tarafı da 2-3 haneli) olduğu gibi kullanılır — bunlara
        /// dokunulmaz. Bu kalıba uymayan (4X25 gibi kısaltılmış) kodlar önce
        /// sizes.txt kaydına bakılır; orada da yoksa askUnknownSize ile
        /// kullanıcıya sorulur, verilen ölçü sizes.txt'ye kaydedilip kullanılır.
        /// Kullanıcı boş geçerse kod aynen döner ve resolved false olur.
        /// </summary>
        private string ResolveSize(string rawSize, out bool resolved)
        {
            resolved = true;

            if (IsStandardSize(rawSize))
                return rawSize;

            if (sizeRegistry.Map.TryGetValue(rawSize, out var known))
                return known;

            if (askUnknownSize != null && !_declinedSizes.Contains(rawSize))
            {
                var answer = askUnknownSize(rawSize);
                if (!string.IsNullOrWhiteSpace(answer))
                {
                    var normalized = NormalizeSize(answer);
                    sizeRegistry.Add(rawSize, normalized);
                    return normalized;
                }
                _declinedSizes.Add(rawSize);
            }

            resolved = false;
            return rawSize;
        }

        /// <summary>Her iki ölçü de 2-3 haneli mi (160X230, 80X150 gibi bilinen biçim).</summary>
        private static bool IsStandardSize(string size)
        {
            var parts = size.Split('X');
            return parts.Length == 2
                && parts[0].Length is 2 or 3 && parts[0].All(char.IsDigit)
                && parts[1].Length is 2 or 3 && parts[1].All(char.IsDigit);
        }

        /// <summary>Kullanıcının yazdığı "400 x 2500" gibi bir metni "400X2500" biçimine çevirir.</summary>
        public static string NormalizeSize(string input) =>
            string.Join("X", input.ToUpperInvariant()
                .Split('X', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        /// <summary>
        /// Üretim emri Excel'i olmayan müşteriler için: elle girilen
        /// Desen+Renk+Kenar satırlarını ve ebat başına adetleri doğrudan
        /// çıktı satırlarına çevirir. Dosyadan okunan akışın aksine (Convert)
        /// renk/ebat/kenar kullanıcı tarafından zaten seçildiğinden çözümleme
        /// yapılmaz; girilen adet doğrudan gerçek parça sayısıdır (alt-üst
        /// çarpanı burada uygulanmaz, pivot tablodaki adet zaten nihaidir).
        /// </summary>
        public ConversionResult ConvertManual(string quality, IEnumerable<ManualOrderRow> rows)
        {
            var result = new ConversionResult();
            long serial = settings.StartingSerial;
            var serialPrefix = CompanyConfig.Load().SerialPrefix();

            foreach (var row in rows)
            {
                var pattern = row.Pattern.Trim();
                var color = row.Color.Trim();
                var edge = row.Edge.Trim();
                if (pattern.Length == 0 || color.Length == 0 || edge.Length == 0) continue;

                var edgeLetter = EdgeOptions.All.FirstOrDefault(o => o.Name == edge).Letter ?? "";

                // UrunKodu ve Desen tamamen büyük harf/ASCII olmalı (Mavi -> MAVI);
                // Renk kolonunda ise okunaklı başlık düzeni gösterilir (Mavi).
                var qualityCode = TurkishText.ToAsciiUpper(quality);
                var patternCode = TurkishText.ToAsciiUpper(pattern);
                var colorCode = TurkishText.ToAsciiUpper(color);
                var colorDisplay = TurkishText.ToTitleCase(color);

                foreach (var (sizeKey, qty) in row.Quantities)
                {
                    if (qty <= 0) continue;

                    var productCode = $"{qualityCode}_{patternCode}_{colorCode}_{sizeKey}{edgeLetter}";
                    var size = sizeKey.Replace("X", "x") + settings.SizeSuffix;
                    var ean = eanRegistry.GetOrCreate(productCode);

                    for (int i = 0; i < qty; i++)
                    {
                        result.Rows.Add(new ProductEntryRow
                        {
                            ProductRoad = row.Road,
                            ProductCode = productCode,
                            Quality = quality,
                            Pattern = patternCode,
                            Color = colorDisplay,
                            Size = size,
                            Edge = edge,
                            Serial = $"{serialPrefix}{serial++}",
                            Ean = ean,
                            ImagePath = row.ImagePath,
                        });
                    }
                }
            }

            // Bir sonraki çalıştırma/liste bıraktığı yerden devam etsin diye
            // son kullanılan seri numarası kalıcı ayarlara geri yazılır.
            settings.StartingSerial = serial;
            settings.Save();

            return result;
        }
    }
}
