using System.Reflection;
using Microsoft.AspNetCore.Http;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Wokki.Api.Apis.Attendance;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Attendance;
using Wokki.Application.Services.Attendance.Interfaces;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Common.Utils;

namespace Wokki.Tests.Unit.Endpoints;

/// <summary>
/// Tests for AttendanceEndpoints private static handlers (ListAsync, AdjustAsync).
/// </summary>
public class AttendanceEndpointsTests
{
    private static readonly Type EndpointType = typeof(AttendanceEndpoints);

    // ─── helpers ────────────────────────────────────────────────────────────

    private static async Task<IResult> InvokeListAsync(
        AttendanceListRequest request,
        IAttendanceService service,
        ICurrentUserService currentUser)
    {
        var method = EndpointType.GetMethod("ListAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var task = (Task<IResult>)method.Invoke(null, [request, service, currentUser, CancellationToken.None])!;
        return await task;
    }

    private static async Task<IResult> InvokeAdjustAsync(
        Guid id,
        AdjustAttendanceRequest request,
        IAttendanceService service,
        ILocationScopeService scopeService,
        IValidator<AdjustAttendanceRequest> validator,
        ICurrentUserService currentUser)
    {
        var method = EndpointType.GetMethod("AdjustAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var task = (Task<IResult>)method.Invoke(null,
            [id, request, service, scopeService, validator, currentUser, CancellationToken.None])!;
        return await task;
    }

    private static int? GetStatusCode(IResult result)
    {
        var prop = result.GetType().GetProperty("StatusCode");
        return prop?.GetValue(result) as int?;
    }

    private static ICurrentUserService AuthenticatedUser(Guid userId, string role)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(m => m.UserId).Returns(userId);
        mock.Setup(m => m.Role).Returns(role);
        return mock.Object;
    }

    private static ICurrentUserService UnauthenticatedUser()
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(m => m.UserId).Returns((Guid?)null);
        mock.Setup(m => m.Role).Returns((string?)null);
        return mock.Object;
    }

    private static IValidator<AdjustAttendanceRequest> ValidValidator()
    {
        var mock = new Mock<IValidator<AdjustAttendanceRequest>>();
        mock.Setup(v => v.Validate(It.IsAny<AdjustAttendanceRequest>()))
            .Returns(new ValidationResult());
        return mock.Object;
    }

    private static IValidator<AdjustAttendanceRequest> InvalidValidator()
    {
        var mock = new Mock<IValidator<AdjustAttendanceRequest>>();
        mock.Setup(v => v.Validate(It.IsAny<AdjustAttendanceRequest>()))
            .Returns(new ValidationResult(new[]
            {
                new ValidationFailure("ClockIn", "ClockIn is required.")
            }));
        return mock.Object;
    }

    private static AdjustAttendanceRequest ValidRequest() =>
        new(DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow, "Test adjustment");

    // ─── ListAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_NullUserId_Returns401()
    {
        var result = await InvokeListAsync(
            new AttendanceListRequest(),
            Mock.Of<IAttendanceService>(),
            UnauthenticatedUser());

        Assert.Equal(401, GetStatusCode(result));
    }

    [Fact]
    public async Task ListAsync_NullRole_Returns401()
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(m => m.UserId).Returns(Guid.NewGuid());
        mock.Setup(m => m.Role).Returns((string?)null);

        var result = await InvokeListAsync(
            new AttendanceListRequest(),
            Mock.Of<IAttendanceService>(),
            mock.Object);

        Assert.Equal(401, GetStatusCode(result));
    }

    [Fact]
    public async Task ListAsync_Authenticated_DelegatesToService()
    {
        var userId = Guid.NewGuid();
        var serviceMock = new Mock<IAttendanceService>();

        var fakeResponse = ApiResponse<PagedResponse<AttendanceResponse>>.SuccessPagedResponse(
            Array.Empty<AttendanceResponse>(), 1, 20, 0, AppMessages.Attendance.Listed);
        serviceMock.Setup(s => s.ListAsync(It.IsAny<AttendanceListRequest>(), default))
                   .ReturnsAsync(fakeResponse);

        var result = await InvokeListAsync(
            new AttendanceListRequest(),
            serviceMock.Object,
            AuthenticatedUser(userId, "Admin"));

        serviceMock.Verify(s => s.ListAsync(It.IsAny<AttendanceListRequest>(), default), Times.Once);
        Assert.Equal(200, GetStatusCode(result));
    }

    // ─── AdjustAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AdjustAsync_InvalidRequest_Returns400()
    {
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();

        var result = await InvokeAdjustAsync(
            id,
            ValidRequest(),
            Mock.Of<IAttendanceService>(),
            Mock.Of<ILocationScopeService>(),
            InvalidValidator(),
            AuthenticatedUser(userId, "Admin"));

        Assert.Equal(400, GetStatusCode(result));
    }

    [Fact]
    public async Task AdjustAsync_NullUserId_Returns401()
    {
        var result = await InvokeAdjustAsync(
            Guid.NewGuid(),
            ValidRequest(),
            Mock.Of<IAttendanceService>(),
            Mock.Of<ILocationScopeService>(),
            ValidValidator(),
            UnauthenticatedUser());

        Assert.Equal(401, GetStatusCode(result));
    }

    [Fact]
    public async Task AdjustAsync_NullRole_Returns401()
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(m => m.UserId).Returns(Guid.NewGuid());
        mock.Setup(m => m.Role).Returns((string?)null);

        var result = await InvokeAdjustAsync(
            Guid.NewGuid(),
            ValidRequest(),
            Mock.Of<IAttendanceService>(),
            Mock.Of<ILocationScopeService>(),
            ValidValidator(),
            mock.Object);

        Assert.Equal(401, GetStatusCode(result));
    }

    [Fact]
    public async Task AdjustAsync_ScopeDenied_Returns403()
    {
        var userId = Guid.NewGuid();
        var attendanceId = Guid.NewGuid();

        var scopeMock = new Mock<ILocationScopeService>();
        scopeMock.Setup(s => s.CanManageAttendanceAsync(userId, "Manager", attendanceId, default))
                 .ReturnsAsync(false);

        var result = await InvokeAdjustAsync(
            attendanceId,
            ValidRequest(),
            Mock.Of<IAttendanceService>(),
            scopeMock.Object,
            ValidValidator(),
            AuthenticatedUser(userId, "Manager"));

        Assert.Equal(403, GetStatusCode(result));
    }

    [Fact]
    public async Task AdjustAsync_ScopeGranted_DelegatesToService()
    {
        var userId = Guid.NewGuid();
        var attendanceId = Guid.NewGuid();

        var scopeMock = new Mock<ILocationScopeService>();
        scopeMock.Setup(s => s.CanManageAttendanceAsync(userId, "Admin", attendanceId, default))
                 .ReturnsAsync(true);

        var serviceMock = new Mock<IAttendanceService>();
        var fakeRecord = new AttendanceResponse(
            attendanceId, Guid.NewGuid(), null,
            DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow,
            120, false, Domain.Enums.AttendanceStatus.Adjusted,
            userId, "Test adjustment", DateTime.UtcNow);
        serviceMock.Setup(s => s.AdjustAsync(attendanceId, It.IsAny<AdjustAttendanceRequest>(), userId, default))
                   .ReturnsAsync(ApiResponse<AttendanceResponse>.SuccessResponse(fakeRecord, AppMessages.Attendance.Adjusted));

        var result = await InvokeAdjustAsync(
            attendanceId,
            ValidRequest(),
            serviceMock.Object,
            scopeMock.Object,
            ValidValidator(),
            AuthenticatedUser(userId, "Admin"));

        serviceMock.Verify(s => s.AdjustAsync(attendanceId, It.IsAny<AdjustAttendanceRequest>(), userId, default), Times.Once);
        Assert.Equal(200, GetStatusCode(result));
    }

    [Fact]
    public async Task AdjustAsync_ScopeNotCalledForInvalidRequest()
    {
        // validation failure should short-circuit before scope check
        var scopeMock = new Mock<ILocationScopeService>();

        var result = await InvokeAdjustAsync(
            Guid.NewGuid(),
            ValidRequest(),
            Mock.Of<IAttendanceService>(),
            scopeMock.Object,
            InvalidValidator(),
            AuthenticatedUser(Guid.NewGuid(), "Admin"));

        scopeMock.Verify(s => s.CanManageAttendanceAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(400, GetStatusCode(result));
    }
}
