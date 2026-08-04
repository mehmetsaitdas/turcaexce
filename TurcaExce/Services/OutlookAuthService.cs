using TurcaExce.Config;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace TurcaExce.Services
{
    /// <summary>
    /// Microsoft 365 (Outlook) hesabıyla girişi yönetir. "Giriş Yap" tıklandığında
    /// gerçek bir Microsoft oturum açma penceresi (sistem tarayıcısı) açılır;
    /// başarılı girişten sonra token, exe klasöründeki şifrelenmiş
    /// msal_token_cache.bin dosyasında saklanır — bir daha giriş istemez,
    /// süresi dolunca MSAL arka planda otomatik yeniler.
    /// </summary>
    public static class OutlookAuthService
    {
        // Microsoft Graph'tan e-posta gönderebilmek için istenen delegated izin.
        private static readonly string[] Scopes = ["Mail.Send", "User.Read"];

        public static string CacheFilePath =>
            Path.Combine(AppPaths.DataDirectory, "msal_token_cache.bin");

        private static IPublicClientApplication BuildApp(EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.AzureAdClientId))
                throw new InvalidOperationException(
                    "Outlook girişi için Azure AD Client ID tanımlı değil. " +
                    "conversion_settings.json içindeki Email.AzureAdClientId alanına, " +
                    "Azure Portal'da oluşturduğunuz uygulama kaydının Client ID'sini girin " +
                    "(Mail.Send izni ve 'Allow public client flows' = Yes gerekir).");

            return PublicClientApplicationBuilder
                .Create(settings.AzureAdClientId)
                .WithAuthority($"https://login.microsoftonline.com/{settings.AzureAdTenantId}")
                .WithRedirectUri("http://localhost")
                .Build();
        }

        private static async Task AttachTokenCacheAsync(IPublicClientApplication app)
        {
            var storageProperties = new StorageCreationPropertiesBuilder(
                    Path.GetFileName(CacheFilePath), Path.GetDirectoryName(CacheFilePath)!)
                .Build();
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(app.UserTokenCache);
        }

        /// <summary>
        /// Microsoft giriş penceresini açar (sistem tarayıcısında). Başarılı
        /// olursa giriş yapan hesabın adresini döner ve settings'e kaydeder.
        /// </summary>
        public static async Task<string> SignInInteractiveAsync(EmailSettings settings)
        {
            var app = BuildApp(settings);
            await AttachTokenCacheAsync(app);

            var result = await app.AcquireTokenInteractive(Scopes)
                .WithUseEmbeddedWebView(false) // sistem tarayıcısında Microsoft giriş sayfası açılır
                .ExecuteAsync();

            settings.SignedInAccount = result.Account.Username;
            return result.Account.Username;
        }

        /// <summary>
        /// Önbellekten sessizce token almayı dener (giriş penceresi açmaz).
        /// Oturum yoksa veya süresi geçmişse null döner.
        /// </summary>
        public static async Task<string?> TryGetTokenSilentAsync(EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.AzureAdClientId)) return null;

            var app = BuildApp(settings);
            await AttachTokenCacheAsync(app);

            var accounts = await app.GetAccountsAsync();
            var account = accounts.FirstOrDefault();
            if (account == null) return null;

            try
            {
                var result = await app.AcquireTokenSilent(Scopes, account).ExecuteAsync();
                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                return null; // yeniden giriş gerekiyor
            }
        }

        /// <summary>Kayıtlı oturumu siler (msal_token_cache.bin temizlenir).</summary>
        public static async Task SignOutAsync(EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.AzureAdClientId)) return;

            var app = BuildApp(settings);
            await AttachTokenCacheAsync(app);

            foreach (var account in await app.GetAccountsAsync())
                await app.RemoveAsync(account);

            settings.SignedInAccount = "";
        }
    }
}
