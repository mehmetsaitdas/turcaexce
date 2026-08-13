namespace TurcaExce.Config
{
    /// <summary>
    /// Kalıcı veri klasörü: %AppData%\TurcaExce. Derleme çıktısı (bin\Debug,
    /// bin\Release) her "Clean"de silinebildiği için ayar/kayıt dosyaları
    /// oraya değil buraya yazılır — rebuild veya yeniden yayınlama verileri
    /// silmez.
    /// </summary>
    public static class AppPaths
    {
        public static string DataDirectory { get; } = CreateDataDirectory();

        /// <summary>
        /// Elle üretim emri (btnManualOrder) çıktılarının klasörü:
        /// %AppData%\TurcaExce\Programlar. Çıktılar programın kurulu olduğu
        /// klasöre (AppContext.BaseDirectory) YAZILMAZ: uygulama ClickOnce ile
        /// dağıtıldığı için kurulum yolu her güncellemede değişiyor ve
        /// kullanıcı önceki günlerin dosyalarına ulaşamıyordu. Buradaki yol
        /// ise sabittir.
        /// </summary>
        public static string ProgramsDirectory => Path.Combine(DataDirectory, "Programlar");

        private static string CreateDataDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "TurcaExce");

            // Proje "ExceToTurca" adından "TurcaExce" olarak yeniden adlandırıldı.
            // Eski adla oluşmuş veri klasörü varsa (EAN/renk/program no kayıtları,
            // ayarlar) yeni klasör henüz yoksa bir kerelik olduğu gibi yeni ada
            // taşınır ki mevcut kayıtlar kaybolmasın.
            var oldDir = Path.Combine(appData, "ExceToTurca");
            if (Directory.Exists(oldDir) && !Directory.Exists(dir))
                Directory.Move(oldDir, dir);

            Directory.CreateDirectory(dir);

            // Eski sürümlerde bin klasörüne yazılmış dosyalar varsa (ve yeni
            // konumda henüz yoksa) bir kerelik buraya taşı ki mevcut EAN/renk
            // kayıtları ve ayarlar kaybolmasın.
            foreach (var name in new[] { "conversion_settings.json", "colors.txt", "ean_registry.txt", "msal_token_cache.bin" })
            {
                var oldPath = Path.Combine(AppContext.BaseDirectory, name);
                var newPath = Path.Combine(dir, name);
                if (File.Exists(oldPath) && !File.Exists(newPath))
                    File.Copy(oldPath, newPath);
            }

            return dir;
        }
    }
}
