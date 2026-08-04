using TurcaExce.Config;

namespace TurcaExce.Services
{
    /// <summary>
    /// Çıktı dosyasını Microsoft Graph API üzerinden e-posta ile gönderir.
    /// Gönderim, "Outlook ile Giriş Yap" ile alınan token'a dayanır; giriş
    /// yapılmamışsa açık bir hata döner (arayüz kullanıcıyı girişe yönlendirir).
    /// </summary>
    public static class EmailSender
    {
        /// <summary>Outlook girişi yapılmış mı (arayüz burada uyarı gösterebilsin diye).</summary>
        public static bool IsConfigured(EmailSettings settings) =>
            !string.IsNullOrWhiteSpace(settings.AzureAdClientId) &&
            !string.IsNullOrWhiteSpace(settings.SignedInAccount);

        public static async Task SendAsync(EmailSettings settings, string recipient,
            string attachmentPath, string subject, string body)
        {
            var token = await OutlookAuthService.TryGetTokenSilentAsync(settings);
            if (token == null)
                throw new InvalidOperationException(
                    "E-posta gönderilemedi: Outlook oturumu bulunamadı. " +
                    "\"Outlook ile Giriş Yap\" butonuyla önce giriş yapın.");

            await GraphMailSender.SendAsync(token, recipient, attachmentPath, subject, body);
        }
    }
}
