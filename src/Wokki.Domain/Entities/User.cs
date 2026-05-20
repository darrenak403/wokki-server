using Wokki.Domain.Constants;

namespace Wokki.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = RoleConstants.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
