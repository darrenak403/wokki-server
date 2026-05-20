namespace Wokki.Application.Features.Users.Dtos;

public sealed record UserResponse(Guid Id, string Email, string Role, DateTime CreatedAt);
