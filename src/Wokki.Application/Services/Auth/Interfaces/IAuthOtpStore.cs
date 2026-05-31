namespace Wokki.Application.Services.Auth.Interfaces;

public sealed record AuthOtpEntry(
    string CodeHash,
    int AttemptCount,
    DateTime? VerifiedAtUtc,
    DateTime CreatedAtUtc);

public sealed record AuthOtpSendLimitEntry(
    int SendCount,
    DateTime? LockedUntilUtc,
    DateTime? LastSentAtUtc);

public interface IAuthOtpStore
{
    Task<bool> HasLiveOtpAsync(string email, CancellationToken cancellationToken = default);
    Task SaveOtpAsync(string email, string codeHash, CancellationToken cancellationToken = default);
    Task<AuthOtpEntry?> GetActiveOtpAsync(string email, CancellationToken cancellationToken = default);
    Task UpdateOtpAsync(string email, AuthOtpEntry entry, CancellationToken cancellationToken = default);
    Task MarkVerifiedAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthOtpEntry?> GetVerifiedOtpAsync(string email, CancellationToken cancellationToken = default);
    Task DeleteOtpAsync(string email, CancellationToken cancellationToken = default);

    Task<AuthOtpSendLimitEntry> GetSendLimitAsync(string email, CancellationToken cancellationToken = default);
    Task SaveSendLimitAsync(string email, AuthOtpSendLimitEntry limit, CancellationToken cancellationToken = default);
    Task ResetSendLimitAsync(string email, CancellationToken cancellationToken = default);
}
