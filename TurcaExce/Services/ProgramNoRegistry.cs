using TurcaExce.Config;

namespace TurcaExce.Services
{
    /// <summary>
    /// Kalıcı Program No kayıt defteri. Elle üretim emri girişinde aynı
    /// program numarasının iki kez çalıştırılmasını engellemek için
    /// program_no_registry.txt dosyasında kullanılan numaralar tutulur.
    /// </summary>
    public class ProgramNoRegistry
    {
        public static string FilePath =>
            Path.Combine(AppPaths.DataDirectory, "program_no_registry.txt");

        private readonly HashSet<string> _usedProgramNos = new(StringComparer.OrdinalIgnoreCase);

        private ProgramNoRegistry() { }

        public static ProgramNoRegistry Load()
        {
            var registry = new ProgramNoRegistry();

            if (File.Exists(FilePath))
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length > 0) registry._usedProgramNos.Add(trimmed);
                }

            return registry;
        }

        public bool IsUsed(string programNo) => _usedProgramNos.Contains(programNo.Trim());

        /// <summary>Program numarasını kullanılmış olarak işaretler ve dosyayı hemen günceller.</summary>
        public void MarkUsed(string programNo)
        {
            _usedProgramNos.Add(programNo.Trim());
            Save();
        }

        public void Save() =>
            File.WriteAllLines(FilePath, _usedProgramNos.OrderBy(x => x, StringComparer.Ordinal));
    }
}
