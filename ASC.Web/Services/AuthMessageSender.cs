using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;
using ASC.Web.Configuration;
using MailKit.Security;

namespace ASC.Web.Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private readonly IOptions<ApplicationSettings> _settings;

        public AuthMessageSender(IOptions<ApplicationSettings> settings)
        {
            _settings = settings;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient();
            client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            client.Authenticate("2224801030262@student.tdmu.edu.vn", "uvxtztdcyjgkpwha");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Your Name", "2224801030262@student.tdmu.edu.vn"));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlMessage };

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public Task SendSmsAsync(string number, string message)
        {
            return Task.CompletedTask;
        }
    }
}
