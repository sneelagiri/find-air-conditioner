using System.Net;
using System.Net.Mail;
using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindAirConditioners.Web.Core.Services;

public sealed class EmailNotificationSender(
    IOptions<NotificationOptions> options,
    ILogger<EmailNotificationSender> logger) : IEmailNotificationSender
{
    public async Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var notificationOptions = options.Value;
        if (string.IsNullOrWhiteSpace(notificationOptions.SmtpHost))
        {
            logger.LogInformation("Skipping email send to {RecipientEmail} because no SMTP host is configured.", recipientEmail);
            logger.LogInformation("Email subject: {Subject}", subject);
            logger.LogInformation("Email body:\n{Body}", body);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(notificationOptions.FromAddress, notificationOptions.FromName),
            Subject = subject,
            Body = body
        };
        message.To.Add(recipientEmail);

        using var client = new SmtpClient(notificationOptions.SmtpHost, notificationOptions.SmtpPort)
        {
            EnableSsl = notificationOptions.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(notificationOptions.Username))
        {
            client.Credentials = new NetworkCredential(notificationOptions.Username, notificationOptions.Password);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Sent email notification to {RecipientEmail}.", recipientEmail);
    }
}
