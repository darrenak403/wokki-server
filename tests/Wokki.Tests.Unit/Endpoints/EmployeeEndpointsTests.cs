using System.Reflection;
using Microsoft.AspNetCore.Http;
using Moq;
using Wokki.Api.Apis.Employees;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Employee;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.LocationScope.Interfaces;
using Wokki.Common.Utils;

namespace Wokki.Tests.Unit.Endpoints;

/// <summary>
/// Tests for EmployeeEndpoints private static handlers invoked via reflection.
/// Verifies 401 / 403 / success branching driven by ICurrentUserService and ILocationScopeService.
/// </summary>
public class EmployeeEndpointsTests
{
    // ─── reflection helpers ──────────────────────────────────────────────────

    private static readonly Type EndpointType = typeof(EmployeeEndpoints);

    private static async Task<IResult> InvokeListAsync(
        EmployeeListRequest request,
        IEmployeeService service,
        ILocationScopeService scopeService,
        ICurrentUserService currentUser)
    {
        var method = EndpointType.GetMethod("ListAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var task = (Task<IResult>)method.Invoke(null, [request, service, scopeService, currentUser, CancellationToken.None])!;
        return await task;
    }

    private static async Task<IResult> InvokeGetByIdAsync(
        Guid id,
        IEmployeeService service,
        ILocationScopeService scopeService,
        ICurrentUserService currentUser)
    {
        var method = EndpointType.GetMethod("GetByIdAsync",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var task = (Task<IResult>)method.Invoke(null, [id, service, scopeService, currentUser, CancellationToken.None])!;
        return await task;
    }

    // ─── shared stubs ───────────────────────────────────────────────────────

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

    private static int? GetStatusCode(IResult result)
    {
        var prop = result.GetType().GetProperty("StatusCode");
        return prop?.GetValue(result) as int?;
    }

    // ─── ListAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_NullUserId_Returns401()
    {
        var result = await InvokeListAsync(
            new EmployeeListRequest(),
            Mock.Of<IEmployeeService>(),
            Mock.Of<ILocationScopeService>(),
            UnauthenticatedUser());

        Assert.Equal(401, GetStatusCode(result));
    }

    [Fact]
    public async Task ListAsync_NoFilters_PassesThroughWithoutScopeCheck()
    {
        var userId = Guid.NewGuid();
        var scopeMock = new Mock<ILocationScopeService>();
        var serviceMock = new Mock<IEmployeeService>();

        var fakeResponse = ApiResponse<PagedResponse<EmployeeResponse>>.SuccessPagedResponse(
            Array.Empty<EmployeeResponse>(), 1, 20, 0, AppMessages.Employee.Listed);

        serviceMock.Setup(s => s.ListAsync(It.IsAny<EmployeeListRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(fakeResponse);

        var result = await InvokeListAsync(
            new EmployeeListRequest(),        // no LocationId or DepartmentId
            serviceMock.Object,
            scopeMock.Object,
            AuthenticatedUser(userId, "Admin"));

        // scope methods must NOT have been called
        scopeMock.Verify(s => s.CanManageLocationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        scopeMock.Verify(s => s.CanManageDepartmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(200, GetStatusCode(result));
    }

    [Fact]
    public async Task ListAsync_LocationIdProvided_ScopeDenied_Returns403()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var scopeMock = new Mock<ILocationScopeService>();
        scopeMock.Setup(s => s.CanManageLocationAsync(userId, "Manager", locationId, default))
                 .ReturnsAsync(false);

        var result = await InvokeListAsync(
            new EmployeeListRequest(LocationId: locationId),
            Mock.Of<IEmployeeService>(),
            scopeMock.Object,
            AuthenticatedUser(userId, "Manager"));

        Assert.Equal(403, GetStatusCode(result));
    }

    [Fact]
    public async Task ListAsync_LocationIdProvided_ScopeGranted_Returns200()
    {
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var scopeMock = new Mock<ILocationScopeService>();
        scopeMock.Setup(s => s.CanManageLocationAsync(userId, "Manager", locationId, default))
                 .ReturnsAsync(true);

        var serviceMock = new Mock<IEmployeeService>();
        var fakeResponse = ApiResponse<PagedResponse<EmployeeResponse>>.SuccessPagedResponse(
            Array.Empty<EmployeeResponse>(), 1, 20, 0, AppMessages.Employee.Listed);
        serviceMock.Setup(s => s.ListAsync(It.IsAny<EmployeeListRequest>(), default))
                   .ReturnsAsync(fakeResponse);

        var result = await InvokeListAsync(
            new EmployeeListRequest(LocationId: locationId),
            serviceMock.Object,
            scopeMock.Object,
            AuthenticatedUser(userId, "Manager"));

        Assert.Equal(200, GetStatusCode(result));
    }

    [Fact]
    public async Task ListAsync_DepartmentIdProvided_ScopeDenied_Returns403()
    {
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        var scopeMock = new Mock<ILocationScopeService>();
        scopeMock.Setup(s => s.CanManageDepartmentAsync(userId, "Manager", deptId, default))
                 .ReturnsAsync(false);

        var result = await InvokeListAsync(
            new EmployeeListRequest(DepartmentId: deptId),
            Mock.Of<IEmployeeService>(),
            scopeMock.Object,
            AuthenticatedUser(userId, "Manager"));

        Assert.Equal(403, GetStatusCode(result));
    }

    [Fact]
    public async Task ListAsync_DepartmentIdProvided_ScopeGranted_Returns200()
    {
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        var scopeMock = new Mock<ILocationScopeService>();
        scopeMock.Setup(s => s.CanManageDepartmentAsync(userId, "Manager", deptId, default))
                 .ReturnsAsync(true);

        var serviceMock = new Mock<IEmployeeService>();
        var fakeResponse = ApiResponse<PagedResponse<EmployeeResponse>>.SuccessPagedResponse(
            Array.Empty<EmployeeResponse>(), 1, 20, 0, AppMessages.Employee.Listed);
        serviceMock.Setup(s => s.ListAsync(It.IsAny<EmployeeListRequest>(), default))
                   .ReturnsAsync(fakeResponse);

        var result = await InvokeListAsync(
            new EmployeeListRequest(DepartmentId: deptId),
            serviceMock.Object,
            scopeMock.Object,
            AuthenticatedUser(userId, "Manager"));

        Assert.Equal(200, GetStatusCode(result));
    }

    // ─── GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_NullUserId_Returns401()
    {
        var result = await InvokeGetByIdAsync(
            Guid.NewGuid(),
            Mock.Of<IEmployeeService>(),
            Mock.Of<ILocationScopeService>(),
            UnauthenticatedUser());

        Assert.Equal(401, GetStatusCode(result));
    }

    [Fact]
    public async Task GetByIdAsync_ScopeDenied_Returns403()
    {
        var userId = Guid.NewGuid();
        var empId = Guid.NewGuid();

        var scopeMock = new Mock<ILocationScopeService>();
        scopeMock.Setup(s => s.CanManageEmployeeAsync(userId, "Manager", empId, default))
                 .ReturnsAsync(false);

        var result = await InvokeGetByIdAsync(
            empId,
            Mock.Of<IEmployeeService>(),
            scopeMock.Object,
            AuthenticatedUser(userId, "Manager"));

        Assert.Equal(403, GetStatusCode(result));
    }

    [Fact]
    public async Task GetByIdAsync_ScopeGranted_DelegatesToService()
    {
        var userId = Guid.NewGuid();
        var empId = Guid.NewGuid();

        var scopeMock = new Mock<ILocationScopeService>();
        scopeMock.Setup(s => s.CanManageEmployeeAsync(userId, "Manager", empId, default))
                 .ReturnsAsync(true);

        var serviceMock = new Mock<IEmployeeService>();
        var fakeEmployee = new EmployeeResponse(
            empId, Guid.NewGuid(), "e@e.com", "Manager",
            "First", "Last", "0909", "Dev", 100m,
            Guid.NewGuid(), null, null, null,
            DateTime.UtcNow, null, DateTime.UtcNow);
        serviceMock.Setup(s => s.GetByIdAsync(empId, default))
                   .ReturnsAsync(ApiResponse<EmployeeResponse>.SuccessResponse(fakeEmployee, AppMessages.Employee.Found));

        var result = await InvokeGetByIdAsync(empId, serviceMock.Object, scopeMock.Object, AuthenticatedUser(userId, "Manager"));

        serviceMock.Verify(s => s.GetByIdAsync(empId, default), Times.Once);
        Assert.Equal(200, GetStatusCode(result));
    }
}
