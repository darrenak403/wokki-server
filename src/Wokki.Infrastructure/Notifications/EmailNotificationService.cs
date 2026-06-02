using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Notifications;
using Wokki.Application.Services.Platform.Interfaces;
using Wokki.Domain.Repositories;

namespace Wokki.Infrastructure.Notifications;

public sealed class EmailNotificationService(
    IUnitOfWork unitOfWork,
    IOptions<SmtpSettings> smtpOptions,
    IPlatformDiagnosticState diagnosticState,
    ILogger<EmailNotificationService> logger) : INotificationService
{
    private const string ComponentName = "email";

    public async Task SendAsync(
        Guid userId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var settings = smtpOptions.Value;
        if (!settings.IsConfigured)
            return;

        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
            return;

        var composed = NotificationEmailComposer.Compose(eventName, payload);

        using var message = new MailMessage
        {
            From = new MailAddress(settings.From),
            Subject = composed.Subject,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };
        message.To.Add(user.Email);

        var plainView = AlternateView.CreateAlternateViewFromString(
            composed.PlainTextBody,
            Encoding.UTF8,
            MediaTypeNames.Text.Plain);
        var htmlView = AlternateView.CreateAlternateViewFromString(
            composed.HtmlBody,
            Encoding.UTF8,
            MediaTypeNames.Text.Html);
        message.AlternateViews.Add(plainView);
        message.AlternateViews.Add(htmlView);

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
            logger.LogInformation("Email sent {EventName} to {Email}", eventName, user.Email);
        }
        catch (Exception ex)
        {
            diagnosticState.RecordFailure(ComponentName, ex.GetType().Name, ex.Message, DateTime.UtcNow);
            logger.LogWarning(ex, "Failed to send email {EventName} to {Email}", eventName, user.Email);
            throw;
        }
    }
}
