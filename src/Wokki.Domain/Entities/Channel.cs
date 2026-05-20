using Wokki.Domain.Enums;

namespace Wokki.Domain.Entities;

public class Channel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public ChannelType Type { get; set; } = ChannelType.Direct;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
