using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spim.Application.Common.Interfaces;

namespace Spim.Infrastructure.Email;

public sealed class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.Host))
        {
            logger.LogInformation("Email delivery disabled; skipped message '{Subject}'", subject);
            return;
        }

        using var message = new MailMessage(settings.From, to, subject, body);
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };

        if (!string.IsNullOrEmpty(settings.UserName))
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Email '{Subject}' sent", subject);
    }
}
