using System.Globalization;

namespace TurcaExce.Services
{
    /// <summary>Türkçe karakter içeren metinleri kod/görüntü biçimlerine çevirir.</summary>
    public static class TurkishText
    {
        private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>Büyük harfe çevirir; Türkçe karakterleri ASCII karşılığına indirger (Ç→C, Ğ→G, İ/ı→I, Ö→O, Ş→S, Ü→U). UrunKodu gibi kod alanları için.</summary>
        public static string ToAsciiUpper(string text) =>
            new([.. text.Select(ToAsciiUpper)]);

        public static char ToAsciiUpper(char c) => c switch
        {
            // 'ı' (noktasız küçük i, U+0131) - ToUpperInvariant bunu DEĞİŞTİRMEZ
            // (İ/ı/i/I ayrımı yalnızca Türkçe kültüründe geçerli, invariant'ta yok),
            // bu yüzden aşağıdaki switch'e düşmeden burada elle yakalanmalı.
            'ı' => 'I',
            _ => char.ToUpperInvariant(c) switch
            {
                'Ç' => 'C',
                'Ğ' => 'G',
                'İ' => 'I',
                'Ö' => 'O',
                'Ş' => 'S',
                'Ü' => 'U',
                var upper => upper,
            }
        };

        /// <summary>Görüntüleme için başlık düzeni: "MAVI"/"mavi" -> "Mavi". Türkçe karakterler korunur (Renk gibi görüntü alanları için).</summary>
        public static string ToTitleCase(string text) =>
            TurkishCulture.TextInfo.ToTitleCase(text.ToLower(TurkishCulture));
    }
}
