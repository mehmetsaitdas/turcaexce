using System.Globalization;
using TurcaExce.Config;

namespace TurcaExce.Services
{
    /// <summary>
    /// Gönderilen her e-postanın tarih/saatini, dosya adını ve alıcısını
    /// %AppData%\TurcaExce\email_history.txt dosyasına bir satır olarak ekler
    /// (biçim: "2026-08-09 14:33:12|8503308.xlsx|alici@firma.com"). Dosya
    /// ayar/kayıt klasöründe tutulduğu için rebuild veya yeniden yayınlama
    /// geçmişi silmez (bkz. AppPaths). Kullanıcı bu kaydı "Gönderim Geçmişi"
    /// butonuyla görür (bkz. EmaHistoryForm).
    /// </summary>
    public static class EmailHistory
    {
        public static string FilePath =>
            Path.Combine(AppPaths.DataDirectory, "email_history.txt");

        /// <summary>Dosyaya yazılan biçim: kültürden bağımsız, sıralanabilir ve geri okunabilir.</summary>
        private const string StorageFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Kaydı dosyanın sonuna ekler. Yazma başarısızsa (dosya kilitli, disk
        /// dolu vb.) İSTİSNA FIRLATMAZ, false döner: e-posta bu noktada zaten
        /// gönderilmiş olduğundan geçmiş kaydı hatası "gönderilemedi" gibi
        /// gösterilmemeli; arayüz durum satırında yalnızca uyarır.
        /// </summary>
        public static bool Append(string fileName, string recipient)
        {
            var line = string.Join('|',
                DateTime.Now.ToString(StorageFormat, CultureInfo.InvariantCulture),
                Sanitize(fileName),
                Sanitize(recipient));

            try
            {
                File.AppendAllText(FilePath, line + Environment.NewLine);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Tüm kayıtları EN YENİ EN BAŞTA olacak şekilde döndürür. Dosya yoksa
        /// (hiç gönderim yapılmamış) boş liste döner; biçimi bozuk satırlar
        /// listeyi bozmasın diye sessizce atlanır.
        /// </summary>
        public static List<EmailHistoryEntry> Load()
        {
            var entries = new List<EmailHistoryEntry>();
            if (!File.Exists(FilePath)) return entries;

            foreach (var line in File.ReadAllLines(FilePath))
            {
                var parts = line.Split('|');
                if (parts.Length < 2) continue;

                if (!DateTime.TryParseExact(parts[0].Trim(), StorageFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var sentAt))
                    continue;

                entries.Add(new EmailHistoryEntry(
                    sentAt,
                    parts[1].Trim(),
                    parts.Length > 2 ? parts[2].Trim() : ""));
            }

            // Dosya kronolojik yazılır; ekranda en yeni en başta istendiği için
            // ters çevrilmesi yeterli olurdu, yine de saate göre sıralanıyor ki
            // dosya elle düzenlenmiş olsa bile sıra doğru olsun.
            return [.. entries.OrderByDescending(e => e.SentAt)];
        }

        /// <summary>'|' ayırıcısı ve satır sonu kayıt biçimini bozmasın diye temizlenir.</summary>
        private static string Sanitize(string value) =>
            value.Replace('|', '/').Replace('\r', ' ').Replace('\n', ' ').Trim();
    }

    /// <summary>email_history.txt'deki tek bir gönderim kaydı.</summary>
    public record EmailHistoryEntry(DateTime SentAt, string FileName, string Recipient);
}
