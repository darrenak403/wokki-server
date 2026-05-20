using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Repositories;

namespace Wokki.Infrastructure.Notifications;

public sealed class EmailNotificationService(
    IUnitOfWork unitOfWork,
    IOptions<SmtpSettings> smtpOptions,
    ILogger<EmailNotificationService> logger) : INotificationService
{
    public async Task SendAsync(
        Guid userId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var settings = smtpOptions.Value;
        if (!settings.IsConfigured)
            return;

        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
            return;

        var subject = $"Wokki: {eventName}";
        var body = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        using var message = new MailMessage(settings.From, user.Email, subject, body);
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(settings.Username))
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Email sent {EventName} to {Email}", eventName, user.Email);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send email {EventName} to {Email}", eventName, user.Email);
            throw;
        }
    }
}
