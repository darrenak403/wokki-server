using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wokki.Application.Common.Interfaces;

namespace Wokki.Infrastructure.Notifications;

/// <summary>Wraps SMTP sender — logs OTP body in Development when SMTP is not configured.</summary>
public sealed class DevLoggingTransactionalEmailSender(
    SmtpTransactionalEmailSender inner,
    IHostEnvironment environment,
    IOptions<SmtpSettings> smtpOptions,
    ILogger<DevLoggingTransactionalEmailSender> logger) : ITransactionalEmailSender
{
    public async Task SendAsync(
        string toEmail,
        string subject,
        string plainTextBody,
        CancellationToken cancellationToken = default)
    {
        if (!smtpOptions.Value.IsConfigured && environment.IsDevelopment())
        {
            logger.LogInformation(
                "DEV email (SMTP not configured) → {Email} | {Subject}\n{Body}",
                toEmail,
                subject,
                plainTextBody);
            return;
        }

        await inner.SendAsync(toEmail, subject, plainTextBody, cancellationToken);
    }
}
