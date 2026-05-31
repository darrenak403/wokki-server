namespace Wokki.Application.Dtos.Chat;

public sealed record OrgChatMemberResponse(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string Role,
    bool IsOrgAdmin,
    string? DepartmentName,
    string? LocationName);
