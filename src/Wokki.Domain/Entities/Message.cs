namespace Wokki.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid SenderId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
