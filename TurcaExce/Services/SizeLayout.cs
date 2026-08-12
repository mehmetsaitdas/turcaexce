using TurcaExce.Config;

namespace TurcaExce.Services
{
    /// <summary>
    /// Elle üretim emri ekranındaki ebat sütunlarının son hâli ("şablon").
    /// SizeRegistry (sizes.txt) zamanla öğrenilen tüm ebatları tutar ve hepsi
    /// birden ekrana sığmaz; burada ise kullanıcının en son hangi ebatları
    /// hangi sırayla açık bıraktığı size_layout.txt'de saklanır, pencere bir
    /// dahaki açılışında aynı şablonla gelir.
    /// </summary>
    public class SizeLayout
    {
        public static string FilePath =>
            Path.Combine(AppPaths.DataDirectory, "size_layout.txt");

        public List<string> Sizes { get; } = [];

        private SizeLayout() { }

        public static SizeLayout Load()
        {
            var layout = new SizeLayout();

            if (File.Exists(FilePath))
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var size = line.Trim();
                    if (size.Length > 0 && !layout.Sizes.Contains(size, StringComparer.OrdinalIgnoreCase))
                        layout.Sizes.Add(size);
                }

            return layout;
        }

        /// <summary>Ekrandaki ebat sütunlarını sırasıyla kaydeder (şablonun yeni hâli).</summary>
        public void Save(IEnumerable<string> sizes)
        {
            Sizes.Clear();
            Sizes.AddRange(sizes);
            File.WriteAllLines(FilePath, Sizes);
        }
    }
}
