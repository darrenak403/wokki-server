using Wokki.Domain.Constants;

namespace Wokki.Application.Dtos.Auth;

public sealed record RegisterRequest(string Email, string Password, string Role = RoleConstants.User);
