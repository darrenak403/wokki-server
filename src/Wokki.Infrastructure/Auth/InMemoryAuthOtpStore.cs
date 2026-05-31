using System.Collections.Concurrent;
using Wokki.Application.Services.Auth;
using Wokki.Application.Services.Auth.Interfaces;

namespace Wokki.Infrastructure.Auth;

/// <summary>In-process OTP store for the Testing environment only.</summary>
public sealed class InMemoryAuthOtpStore : IAuthOtpStore
{
    private sealed record StoredOtp(AuthOtpEntry Entry, DateTime ExpiresAtUtc);

    private sealed record StoredSendLimit(AuthOtpSendLimitEntry Entry);

    private readonly ConcurrentDictionary<string, StoredOtp> _otps = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, StoredSendLimit> _sendLimits = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> HasLiveOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        PurgeExpired(email);
        return Task.FromResult(_otps.ContainsKey(Normalize(email)));
    }

    public Task SaveOtpAsync(string email, string codeHash, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(email);
        _otps[normalized] = new StoredOtp(
            new AuthOtpEntry(codeHash, 0, null, DateTime.UtcNow),
            DateTime.UtcNow.Add(AuthOtpHelper.Expiry));
        return Task.CompletedTask;
    }

    public Task<AuthOtpEntry?> GetActiveOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        PurgeExpired(email);
        if (!_otps.TryGetValue(Normalize(email), out var stored) || stored.Entry.VerifiedAtUtc is not null)
            return Task.FromResult<AuthOtpEntry?>(null);

        return Task.FromResult<AuthOtpEntry?>(stored.Entry);
    }

    public Task UpdateOtpAsync(string email, AuthOtpEntry entry, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(email);
        if (_otps.TryGetValue(normalized, out var stored))
            _otps[normalized] = stored with { Entry = entry };

        return Task.CompletedTask;
    }

    public Task MarkVerifiedAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(email);
        if (!_otps.TryGetValue(normalized, out var stored))
            return Task.CompletedTask;

        _otps[normalized] = new StoredOtp(
            stored.Entry with { VerifiedAtUtc = DateTime.UtcNow },
            DateTime.UtcNow.Add(AuthOtpHelper.VerifiedWindow));
        return Task.CompletedTask;
    }

    public Task<AuthOtpEntry?> GetVerifiedOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        PurgeExpired(email);
        if (!_otps.TryGetValue(Normalize(email), out var stored) || stored.Entry.VerifiedAtUtc is null)
            return Task.FromResult<AuthOtpEntry?>(null);

        var cutoff = DateTime.UtcNow.Subtract(AuthOtpHelper.VerifiedWindow);
        return Task.FromResult<AuthOtpEntry?>(
            stored.Entry.VerifiedAtUtc > cutoff ? stored.Entry : null);
    }

    public Task DeleteOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        _otps.TryRemove(Normalize(email), out _);
        return Task.CompletedTask;
    }

    public Task<AuthOtpSendLimitEntry> GetSendLimitAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!_sendLimits.TryGetValue(Normalize(email), out var stored))
            return Task.FromResult(new AuthOtpSendLimitEntry(0, null, null));

        var now = DateTime.UtcNow;
        if (stored.Entry.LockedUntilUtc is not null && stored.Entry.LockedUntilUtc <= now)
            return Task.FromResult(new AuthOtpSendLimitEntry(0, null, stored.Entry.LastSentAtUtc));

        return Task.FromResult(stored.Entry);
    }

    public Task SaveSendLimitAsync(string email, AuthOtpSendLimitEntry limit, CancellationToken cancellationToken = default)
    {
        _sendLimits[Normalize(email)] = new StoredSendLimit(limit);
        return Task.CompletedTask;
    }

    public Task ResetSendLimitAsync(string email, CancellationToken cancellationToken = default)
    {
        _sendLimits.TryRemove(Normalize(email), out _);
        return Task.CompletedTask;
    }

    private void PurgeExpired(string email)
    {
        var normalized = Normalize(email);
        if (_otps.TryGetValue(normalized, out var stored) && stored.ExpiresAtUtc <= DateTime.UtcNow)
            _otps.TryRemove(normalized, out _);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
