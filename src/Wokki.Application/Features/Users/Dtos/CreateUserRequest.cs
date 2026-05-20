using Wokki.Domain.Constants;

namespace Wokki.Application.Features.Users.Dtos;

public sealed record CreateUserRequest(string Email, string Password, string Role = RoleConstants.User);
