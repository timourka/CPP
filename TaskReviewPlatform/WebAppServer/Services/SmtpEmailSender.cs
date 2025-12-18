using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models.Models;
using System.Net;
using System.Net.Mail;

namespace WebAppServer.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task SendEmailAsync(User user, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogInformation("Skip email notification for user {Login}: empty email", user.Login);
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.Host))
            {
                _logger.LogWarning("Email host is not configured. Skipping email for user {Login}", user.Login);
                return;
            }

            var fromAddress = new MailAddress(string.IsNullOrWhiteSpace(_options.From) ? _options.Username ?? "noreply@example.com" : _options.From);
            var toAddress = new MailAddress(user.Email);

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            try
            {
                await client.SendMailAsync(message);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", user.Email);
            }
        }
    }
}
