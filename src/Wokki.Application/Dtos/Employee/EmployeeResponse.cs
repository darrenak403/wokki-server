namespace Wokki.Application.Dtos.Employee;

public sealed record EmployeeResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string Role,
    string FirstName,
    string LastName,
    string Phone,
    string Position,
    decimal HourlyRate,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? LocationId,
    string? LocationName,
    string? BankAccountNumber,
    string? BankAccountHolderName,
    string? BankName,
    string? PaymentQrImageUrl,
    DateTime EmployedAt,
    DateTime? TerminatedAt,
    DateTime CreatedAt,
    bool HasFaceEnrollment = false);
