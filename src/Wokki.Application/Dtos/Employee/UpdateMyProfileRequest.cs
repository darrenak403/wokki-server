namespace Wokki.Application.Dtos.Employee;

public sealed record UpdateMyProfileRequest(
    string FirstName,
    string LastName,
    string? Phone,
    string? BankAccountNumber,
    string? BankAccountHolderName,
    string? BankName,
    bool RemovePaymentQr = false);
