using Wokki.Application.Dtos.Employee;
using Wokki.Domain.Entities;

namespace Wokki.Application.Mappings.Employees;

public static class EmployeeMapper
{
    public static EmployeeResponse ToResponse(
        this Employee employee,
        User user,
        Department? department = null,
        Location? location = null) =>
        new(
            employee.Id,
            employee.UserId,
            user.Email,
            user.Role,
            employee.FirstName,
            employee.LastName,
            employee.Phone,
            employee.Position,
            employee.HourlyRate,
            employee.DepartmentId,
            department?.Name,
            location?.Id,
            location?.Name,
            employee.BankAccountNumber,
            employee.BankAccountHolderName,
            employee.BankName,
            employee.PaymentQrImageUrl,
            employee.EmployedAt,
            employee.TerminatedAt,
            employee.CreatedAt,
            employee.FaceEnrollmentPhotoUrl != null);

    public static Employee ToEntity(this CreateEmployeeRequest request, Guid userId, Guid organizationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone?.Trim() ?? string.Empty,
            HourlyRate = request.HourlyRate,
            DepartmentId = request.DepartmentId,
            EmployedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

    public static void ApplyUpdate(this Employee employee, UpdateEmployeeRequest request)
    {
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Phone = request.Phone?.Trim() ?? string.Empty;
        employee.HourlyRate = request.HourlyRate;
        employee.DepartmentId = request.DepartmentId;
    }

    public static void ApplyMyProfileUpdate(this Employee employee, UpdateMyProfileRequest request)
    {
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Phone = request.Phone?.Trim() ?? string.Empty;
        employee.BankAccountNumber = NormalizeOptional(request.BankAccountNumber);
        employee.BankAccountHolderName = NormalizeOptional(request.BankAccountHolderName);
        employee.BankName = NormalizeOptional(request.BankName);
    }

    public static void ClearPaymentQr(this Employee employee)
    {
        employee.PaymentQrImageUrl = null;
        employee.PaymentQrPublicId = null;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim();
    }

    /// <summary>Scheduling solver reads Position — kept in sync with primary department name.</summary>
    public static void SyncPositionFromDepartment(Employee employee, Department department) =>
        employee.Position = department.Name.Trim();
}
