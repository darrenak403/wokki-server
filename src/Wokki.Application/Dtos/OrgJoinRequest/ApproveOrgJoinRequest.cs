namespace Wokki.Application.Dtos.OrgJoinRequest;

public sealed record ApproveOrgJoinRequest(Guid DepartmentId, decimal HourlyRate, string? Phone);
