using System.Text.Json;
using StackExchange.Redis;
using Wokki.Application.Services.Auth;
using Wokki.Application.Services.Auth.Interfaces;

namespace Wokki.Infrastructure.Auth;

public sealed class RedisAuthOtpStore(IConnectionMultiplexer redis) : IAuthOtpStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _db = redis.GetDatabase();

    private static string OtpKey(string email) => $"wokki:auth:otp:{Normalize(email)}";
    private static string SendKey(string email) => $"wokki:auth:otp:send:{Normalize(email)}";

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public Task<bool> HasLiveOtpAsync(string email, CancellationToken cancellationToken = default) =>
        _db.KeyExistsAsync(OtpKey(email));

    public Task SaveOtpAsync(string email, string codeHash, CancellationToken cancellationToken = default)
    {
        var entry = new AuthOtpEntry(codeHash, 0, null, DateTime.UtcNow);
        return SetOtpAsync(email, entry, AuthOtpHelper.Expiry);
    }

    public async Task<AuthOtpEntry?> GetActiveOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var entry = await GetOtpAsync(email);
        return entry is null or { VerifiedAtUtc: not null } ? null : entry;
    }

    public async Task UpdateOtpAsync(string email, AuthOtpEntry entry, CancellationToken cancellationToken = default)
    {
        var ttl = await _db.KeyTimeToLiveAsync(OtpKey(email));
        var expiry = ttl is not null && ttl.Value > TimeSpan.Zero ? ttl.Value : AuthOtpHelper.Expiry;
        await SetOtpAsync(email, entry, expiry);
    }

    public async Task MarkVerifiedAsync(string email, CancellationToken cancellationToken = default)
    {
        var entry = await GetOtpAsync(email);
        if (entry is null)
            return;

        await SetOtpAsync(
            email,
            entry with { VerifiedAtUtc = DateTime.UtcNow },
            AuthOtpHelper.VerifiedWindow);
    }

    public async Task<AuthOtpEntry?> GetVerifiedOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var entry = await GetOtpAsync(email);
        if (entry?.VerifiedAtUtc is null)
            return null;

        var cutoff = DateTime.UtcNow.Subtract(AuthOtpHelper.VerifiedWindow);
        return entry.VerifiedAtUtc > cutoff ? entry : null;
    }

    public Task DeleteOtpAsync(string email, CancellationToken cancellationToken = default) =>
        _db.KeyDeleteAsync(OtpKey(email));

    public async Task<AuthOtpSendLimitEntry> GetSendLimitAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var value = await _db.StringGetAsync(SendKey(email));
        if (value.IsNullOrEmpty)
            return new AuthOtpSendLimitEntry(0, null, null);

        var entry = JsonSerializer.Deserialize<AuthOtpSendLimitEntry>(value.ToString(), JsonOptions)
                    ?? new AuthOtpSendLimitEntry(0, null, null);

        var now = DateTime.UtcNow;
        if (entry.LockedUntilUtc is not null && entry.LockedUntilUtc <= now)
            return new AuthOtpSendLimitEntry(0, null, entry.LastSentAtUtc);

        return entry;
    }

    public Task SaveSendLimitAsync(
        string email,
        AuthOtpSendLimitEntry limit,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(limit, JsonOptions);
        return _db.StringSetAsync(SendKey(email), json);
    }

    public Task ResetSendLimitAsync(string email, CancellationToken cancellationToken = default) =>
        _db.KeyDeleteAsync(SendKey(email));

    private async Task<AuthOtpEntry?> GetOtpAsync(string email)
    {
        var value = await _db.StringGetAsync(OtpKey(email));
        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<AuthOtpEntry>(value.ToString(), JsonOptions);
    }

    private Task SetOtpAsync(string email, AuthOtpEntry entry, TimeSpan expiry) =>
        _db.StringSetAsync(OtpKey(email), JsonSerializer.Serialize(entry, JsonOptions), expiry);
}
