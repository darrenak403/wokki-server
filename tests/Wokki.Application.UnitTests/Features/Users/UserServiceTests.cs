using Microsoft.AspNetCore.Http;
using Moq;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Services.User.Implementations;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Constants;
using Wokki.Domain.Repositories;

namespace Wokki.Application.UnitTests.Features.Users;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    public UserServiceTests()
    {
        _uow.Setup(u => u.Users).Returns(_users.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenUserMissing()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(_uow.Object, _passwordHasher.Object);
        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(AppMessages.User.NotFound.Code, result.Message.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Message.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSuccess_WhenUserExists()
    {
        var id = Guid.NewGuid();
        _users.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = id, Email = "a@b.com", Role = RoleConstants.User });

        var service = new UserService(_uow.Object, _passwordHasher.Object);
        var result = await service.GetByIdAsync(id);

        Assert.True(result.Success);
        Assert.Equal(AppMessages.User.Found.Code, result.Message.Code);
        Assert.NotNull(result.Data);
        Assert.Equal(id, result.Data.Id);
    }
}
