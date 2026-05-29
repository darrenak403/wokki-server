# Cache pattern (phase 2+)

Use **cache-aside**:

1. Read: check cache → on miss, load DB → set cache
2. Write: update DB → **invalidate** cache keys (do not rely on TTL alone for critical data)

`ICacheService` is registered in DI. Replace `MemoryCacheService` with `RedisCacheService` when general app caching moves to Redis.

**Auth OTP** uses dedicated `IAuthOtpStore` / `RedisAuthOtpStore` (keys `wokki:auth:otp:{email}`) with TTL 1 minute; delete on successful forgot-password completion. Send-rate limits use `wokki:auth:otp:send:{email}`.
