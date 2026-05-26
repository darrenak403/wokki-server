using Wokki.Domain.Entities;

namespace Wokki.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);
    Guid? ValidateRefreshToken(string refreshToken);
    void RevokeRefreshToken(Guid userId);
}
