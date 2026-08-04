namespace TurcaExce.Config
{
    /// <summary>E-posta gönderim ayarları (conversion_settings.json içinde saklanır).</summary>
    public class EmailSettings
    {
        /// <summary>Kayıtlı alıcı adresi; arayüzde bir kez yazılınca hatırlanır.</summary>
        public string Recipient { get; set; } = "";

        // --- Outlook (Microsoft 365 / kişisel hesap) girişi ---
        // "Outlook ile Giriş Yap" bir Microsoft giriş penceresi açar; kullanıcı
        // orada oturum açtıktan sonra Microsoft Graph API üzerinden token ile
        // e-posta gönderilir. Şifre hiçbir zaman diske yazılmaz — sadece
        // şifrelenmiş token önbelleği (msal_token_cache.bin) ve giriş yapan
        // hesabın adresi (bilgi amaçlı) burada tutulur.
        //
        // ClientId, Azure Portal'da oluşturulan bir "App registration"a aittir
        // (Mail.Send izni + "Allow public client flows" = Yes + hesap türü
        // "kişisel Microsoft hesapları"nı da içerecek şekilde ayarlanmış olmalı).
        // Azure Portal'da oluşturulan uygulama kaydının Client ID'si (Azure'daki
        // kayıt adı projeyle birlikte otomatik değişmez; hâlâ eski adla kayıtlıysa
        // Azure Portal'dan ayrıca yeniden adlandırılması gerekir).
        // Gizli bir değer değildir (public client, secret kullanmaz), bu yüzden
        // koda gömülü — dosya silinse/rebuild olsa bile giriş tekrar bozulmaz.
        public string AzureAdClientId { get; set; } = "24e2225d-224e-4026-bb8c-f7fc0621a757";
        public string AzureAdTenantId { get; set; } = "common";
        public string SignedInAccount { get; set; } = "";
    }
}
