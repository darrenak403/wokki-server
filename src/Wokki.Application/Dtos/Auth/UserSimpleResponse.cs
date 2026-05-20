namespace Wokki.Application.Dtos.Auth;

public sealed record UserSimpleResponse(Guid Id, string Email, string Role);
