using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MarkTogether.Server.Services
{
    public static class EmailService
    {
        public static async Task SendOtpAsync(string toEmail, string username, string otp)
        {
            string host = ConfigurationManager.AppSettings["SmtpHost"];
            int port;
            if (!int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out port) || port <= 0)
                port = 2525;

            string user = ConfigurationManager.AppSettings["SmtpUser"];
            string pass = ConfigurationManager.AppSettings["SmtpPassword"];
            string fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"];
            string fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "MarkTogether";
            bool ssl = (ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true")
                .Equals("true", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                throw new InvalidOperationException("SMTP chưa được cấu hình trên server.");

            if (string.IsNullOrWhiteSpace(fromEmail))
                fromEmail = user;

            using (var client = new SmtpClient(host, port))
            {
                client.EnableSsl = ssl;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(user, pass);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Timeout = 15000;

                using (var msg = new MailMessage(new MailAddress(fromEmail, fromName), new MailAddress(toEmail)))
                {
                    msg.Subject = "MarkTogether — Mã xác thực đặt lại mật khẩu";
                    msg.Body = $"Xin chào {username},\n\nMã xác thực: {otp}\n" +
                               "Mã có hiệu lực 10 phút.\nNếu bạn không yêu cầu, hãy bỏ qua email này.\n\n— MarkTogether";
                    msg.IsBodyHtml = false;

                    await client.SendMailAsync(msg).ConfigureAwait(false);
                }
            }
        }
    }
}