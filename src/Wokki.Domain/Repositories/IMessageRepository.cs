using Wokki.Domain.Entities;

namespace Wokki.Domain.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Message> Items, bool HasMore)> ListByChannelAsync(
        Guid channelId,
        DateTime? beforeCreatedAt,
        int limit,
        CancellationToken cancellationToken = default);
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    void Update(Message message);
}
