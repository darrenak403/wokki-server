using Wokki.Application.Dtos.User;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Users;

public static class UserMapper
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Email, user.Role, user.CreatedAt);

    public static User ToEntity(this CreateUserRequest request, Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = string.Empty,
            Role = request.Role,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };
}
