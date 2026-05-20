namespace Wokki.Application.Dtos.User;

public sealed record UserResponse(Guid Id, string Email, string Role, DateTime CreatedAt);
