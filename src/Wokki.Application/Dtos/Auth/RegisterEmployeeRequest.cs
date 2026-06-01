namespace Wokki.Application.Dtos.Auth;

public sealed record RegisterEmployeeRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone);
