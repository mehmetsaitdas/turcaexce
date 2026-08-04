using TurcaExce.Config;

namespace TurcaExce.Services
{
    /// <summary>
    /// Bu makineden simdiye kadar uretilmis TUM Serial (Ser) numaralarinin kalici
    /// kaydi. Uygulama klasorundeki serial_registry.txt dosyasinda, her satirda
    /// bir numara olarak tutulur. StartingSerial sayaci normalde hep ileri
    /// gittiginden tek basina yeterli olurdu, ama sayacin manuel duzenleme,
    /// eski yedek geri yukleme veya benzeri bir sebeple geriye dusmesi halinde
    /// bile bu kayit sayesinde daha once verilmis bir numara IKINCI KEZ ASLA
    /// atanmaz; cakisma tespit edilirse otomatik olarak bir sonraki bos
    /// numaraya gecilir (bkz. TakeNext).
    /// </summary>
    public class SerialRegistry
    {
        public static string FilePath =>
            Path.Combine(AppPaths.DataDirectory, "serial_registry.txt");

        private readonly HashSet<long> _used = [];
        private bool _dirty;

        private SerialRegistry() { }

        public static SerialRegistry Load()
        {
            var registry = new SerialRegistry();

            if (File.Exists(FilePath))
                foreach (var line in File.ReadAllLines(FilePath))
                    if (long.TryParse(line.Trim(), out var number))
                        registry._used.Add(number);

            return registry;
        }

        /// <summary>
        /// candidate'ten baslayarak daha once hic kullanilmamis ilk numarayi
        /// dondurur, kayida ekler ve candidate'i bir sonraki denemeye hazirlar.
        /// Cakisma (candidate zaten kullanilmis) varsa otomatik olarak bir
        /// sonraki bos numaraya atlar.
        /// </summary>
        public long TakeNext(ref long candidate)
        {
            while (_used.Contains(candidate))
                candidate++;

            _used.Add(candidate);
            _dirty = true;

            return candidate++;
        }

        public void Save()
        {
            if (!_dirty) return;
            File.WriteAllLines(FilePath, _used.OrderBy(n => n).Select(n => n.ToString()));
            _dirty = false;
        }
    }
}
