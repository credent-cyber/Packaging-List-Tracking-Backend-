using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace TechnoPackaginListTracking.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailSender(string smtpServer, int smtpPort, string smtpUser, string smtpPass, ILogger<EmailSender> logger)
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _smtpUser = smtpUser;
            _smtpPass = smtpPass;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            MailMessage message = new MailMessage();
            SmtpClient smtp = new SmtpClient(_smtpServer, _smtpPort);
            message.From = new MailAddress(_smtpUser);
            message.To.Add(new MailAddress(email));
            message.Subject = subject;
            message.IsBodyHtml = true;
            message.Body = htmlMessage;

            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

            try
            {
                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                throw;
            }
        }
    }
}
