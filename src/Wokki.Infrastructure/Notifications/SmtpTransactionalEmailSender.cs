using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Services.Platform.Interfaces;

namespace Wokki.Infrastructure.Notifications;

public sealed class SmtpTransactionalEmailSender(
    IOptions<SmtpSettings> smtpOptions,
    IPlatformDiagnosticState diagnosticState,
    ILogger<SmtpTransactionalEmailSender> logger) : ITransactionalEmailSender
{
    private const string ComponentName = "email";

    public async Task SendAsync(
        string toEmail,
        string subject,
        string plainTextBody,
        CancellationToken cancellationToken = default)
    {
        var settings = smtpOptions.Value;
        if (!settings.IsConfigured)
        {
            logger.LogWarning(
                "SMTP not configured — email to {Email} with subject {Subject} was not sent.",
                toEmail,
                subject);
            return;
        }

        using var message = new MailMessage(settings.From, toEmail, subject, plainTextBody);
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(settings.Username, settings.Password),
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Transactional email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            diagnosticState.RecordFailure(ComponentName, ex.GetType().Name, ex.Message, DateTime.UtcNow);
            throw;
        }
    }
}
