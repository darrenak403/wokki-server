namespace Wokki.Domain.Entities;

public class ChannelMember
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
}
