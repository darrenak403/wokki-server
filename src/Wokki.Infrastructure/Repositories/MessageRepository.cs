using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using Wokki.Infrastructure.Persistence;

namespace Wokki.Infrastructure.Repositories;

public sealed class MessageRepository(AppDbContext context) : IMessageRepository
{
    public async Task<Message?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? context.Messages : context.Messages.AsNoTracking();
        return await query.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Message> Items, bool HasMore)> ListByChannelAsync(
        Guid channelId,
        DateTime? beforeCreatedAt,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = limit is < 1 or > 100 ? 50 : limit;
        var query = context.Messages.AsNoTracking().Where(m => m.ChannelId == channelId);

        if (beforeCreatedAt.HasValue)
            query = query.Where(m => m.CreatedAt < beforeCreatedAt.Value);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(take + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > take;
        if (hasMore)
            items = items.Take(take).ToList();

        return (items, hasMore);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default) =>
        await context.Messages.AddAsync(message, cancellationToken);

    public void Update(Message message) => context.Messages.Update(message);

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetLatestCreatedAtByChannelsAsync(
        IEnumerable<Guid> channelIds,
        CancellationToken cancellationToken = default)
    {
        var ids = channelIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, DateTime>();

        var rows = await context.Messages.AsNoTracking()
            .Where(m => ids.Contains(m.ChannelId))
            .GroupBy(m => m.ChannelId)
            .Select(g => new { ChannelId = g.Key, Latest = g.Max(m => m.CreatedAt) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ChannelId, r => r.Latest);
    }
}
