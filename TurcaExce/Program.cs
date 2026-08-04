using TurcaExce.Config;
using TurcaExce.Services;

namespace TurcaExce
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // Komut satırı modu: TurcaExce.exe --cli <kaynak.xls> <hedef.xlsx>
            // WinExe olduğundan konsol çıktısı yerine <hedef>.log dosyası yazılır.
            if (args.Length >= 3 && args[0].Equals("--cli", StringComparison.OrdinalIgnoreCase))
                return RunCli(args[1], args[2]);

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
            return 0;
        }

        private static int RunCli(string sourcePath, string targetPath)
        {
            try
            {
                var settings = ConversionSettings.Load();
                var eanRegistry = EanRegistry.Load(settings.StartingEan);
                // CLI modunda soru sorulamaz; bilinmeyen kodlar uyarı olarak loglanır.
                var colorRegistry = ColorRegistry.Load(settings.ColorMap);
                var sizeRegistry = SizeRegistry.Load();
                var serialRegistry = SerialRegistry.Load();
                var sourceLines = ProductionOrderReader.Read(sourcePath);
                var result = new ConversionService(settings, eanRegistry, colorRegistry, sizeRegistry, serialRegistry).Convert(sourceLines);
                ExcelWriter.Write(targetPath, result.Rows);
                eanRegistry.Save();

                File.WriteAllLines(targetPath + ".log",
                [
                    $"Kaynak desen satırı : {sourceLines.Count}",
                    $"Yazılan çıktı satırı: {result.Rows.Count}",
                    $"Uyarı sayısı        : {result.Warnings.Count}",
                    .. result.Warnings,
                ]);
                return 0;
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(targetPath + ".log", "HATA: " + ex); } catch { }
                return 1;
            }
        }
    }
}
