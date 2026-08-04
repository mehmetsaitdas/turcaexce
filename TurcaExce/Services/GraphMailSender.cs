using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TurcaExce.Services
{
    /// <summary>
    /// Microsoft Graph API üzerinden (POST /me/sendMail) e-posta gönderir.
    /// Kullanıcı adı/şifre veya SMTP sunucu bilgisi gerekmez; OutlookAuthService
    /// ile alınan erişim token'ı yeterlidir.
    /// </summary>
    public static class GraphMailSender
    {
        private static readonly HttpClient Http = new();

        public static async Task SendAsync(string accessToken, string recipient,
            string attachmentPath, string subject, string body)
        {
            var attachmentBytes = await File.ReadAllBytesAsync(attachmentPath);
            var fileName = Path.GetFileName(attachmentPath);

            // Graph "@odata.type" alanı C# özellik adı olamadığından JSON elle üretilir.
            var json = BuildSendMailJson(recipient, subject, body, fileName, attachmentBytes);

            using var request = new HttpRequestMessage(HttpMethod.Post,
                "https://graph.microsoft.com/v1.0/me/sendMail")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Microsoft Graph e-posta gönderemedi ({(int)response.StatusCode}): {error}");
            }
        }

        private static string BuildSendMailJson(string recipient, string subject, string body,
            string fileName, byte[] attachmentBytes)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteStartObject("message");
                writer.WriteString("subject", subject);

                writer.WriteStartObject("body");
                writer.WriteString("contentType", "Text");
                writer.WriteString("content", body);
                writer.WriteEndObject();

                writer.WriteStartArray("toRecipients");
                writer.WriteStartObject();
                writer.WriteStartObject("emailAddress");
                writer.WriteString("address", recipient);
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndArray();

                writer.WriteStartArray("attachments");
                writer.WriteStartObject();
                writer.WriteString("@odata.type", "#microsoft.graph.fileAttachment");
                writer.WriteString("name", fileName);
                writer.WriteString("contentBytes", Convert.ToBase64String(attachmentBytes));
                writer.WriteEndObject();
                writer.WriteEndArray();

                writer.WriteEndObject(); // message
                writer.WriteBoolean("saveToSentItems", true);
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
