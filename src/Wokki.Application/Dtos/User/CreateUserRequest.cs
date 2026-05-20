using Wokki.Domain.Constants;

namespace Wokki.Application.Dtos.User;

public sealed record CreateUserRequest(string Email, string Password, string Role = RoleConstants.User);
