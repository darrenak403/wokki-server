namespace Wokki.Application.Common.Interfaces;

public interface ITransactionalEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string plainTextBody,
        CancellationToken cancellationToken = default);
}
