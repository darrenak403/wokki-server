using Wokki.Application.Common.Interfaces;
using Wokki.Application.Dtos.Auth;
using Wokki.Application.Services.Auth;
using Wokki.Application.Services.Auth.Interfaces;
using Wokki.Application.Services.Chat.Interfaces;
using Wokki.Application.Services.Employee.Interfaces;
using Wokki.Application.Services.OrganizationSubscription.Interfaces;
using Wokki.Common.Utils;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using UserEntity = Wokki.Domain.Entities.User;
using OrganizationEntity = Wokki.Domain.Entities.Organization;

namespace Wokki.Application.Services.Auth.Implementations;

public sealed class AuthService(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher,
    IOrganizationSubscriptionService organizationSubscription,
    ITransactionalEmailSender emailSender,
    IAuthOtpStore otpStore,
    IOrgAdminEmployeeProvisioner orgAdminEmployeeProvisioner,
    IOrgChannelService orgChannelService) : IAuthService
{
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return ApiResponse<LoginResponse>.FailureResponse(AppMessages.Auth.InvalidCredentials);

        var accessFailure = await organizationSubscription.GetAccessFailureAsync(
            user.OrganizationId,
            user.Role,
            cancellationToken);
        if (accessFailure is not null)
            return ApiResponse<LoginResponse>.FailureResponse(accessFailure);

        return ApiResponse<LoginResponse>.SuccessResponse(
            BuildLoginResponse(user),
            AppMessages.Auth.LoginSuccess);
    }

    public Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var userId = jwtTokenService.ValidateRefreshToken(request.RefreshToken);
        if (userId is null)
            return Task.FromResult(ApiResponse<LoginResponse>.FailureResponse(AppMessages.Auth.Unauthorized));

        return RefreshForUserAsync(userId.Value, cancellationToken);
    }

    public async Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await unitOfWork.Users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
            return ApiResponse<LoginResponse>.FailureResponse(AppMessages.User.Exists);

        var organization = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Name = request.OrganizationName.Trim(),
            IsActive = true,
            SubscriptionEnabled = false,
            SubscriptionDurationDays = 0,
            CreatedAt = DateTime.UtcNow
        };

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = RoleConstants.Admin,
            OrganizationId = organization.Id,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.Organizations.AddAsync(organization, cancellationToken);
            await unitOfWork.Users.AddAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var adminEmployee = await orgAdminEmployeeProvisioner.EnsureAsync(user, cancellationToken);
            await orgChannelService.EnsureOrgChannelAsync(organization.Id, user.Id, cancellationToken);
            if (adminEmployee is not null)
                await orgChannelService.EnsureMemberAsync(organization.Id, adminEmployee.Id, cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return ApiResponse<LoginResponse>.SuccessResponse(
            BuildLoginResponse(user),
            AppMessages.User.Created);
    }

    public async Task<ApiResponse<LoginResponse>> RegisterEmployeeAsync(
        RegisterEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await unitOfWork.Users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
            return ApiResponse<LoginResponse>.FailureResponse(AppMessages.User.Exists);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = RoleConstants.User,
            OrganizationId = null,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<LoginResponse>.SuccessResponse(
            BuildLoginResponse(user),
            AppMessages.Auth.RegisterEmployeeSuccess);
    }

    public async Task<ApiResponse<UserSimpleResponse>> ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.Auth.PasswordConfirmMismatch);

        var user = await unitOfWork.Users.GetByIdAsync(userId, track: true, cancellationToken: cancellationToken);
        if (user is null)
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.Auth.InvalidCredentials);

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<UserSimpleResponse>.SuccessResponse(
            ToUserSimple(user),
            AppMessages.Auth.PasswordChanged);
    }

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var sendLimit = await otpStore.GetSendLimitAsync(email, cancellationToken);
        if (sendLimit.LockedUntilUtc is not null && sendLimit.LockedUntilUtc > now)
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpSendLocked);

        if (await otpStore.HasLiveOtpAsync(email, cancellationToken))
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpResendTooSoon);

        if (sendLimit.SendCount >= AuthOtpHelper.MaxSendAttempts)
        {
            await otpStore.SaveSendLimitAsync(
                email,
                sendLimit with { LockedUntilUtc = now.Add(AuthOtpHelper.SendLockout) },
                cancellationToken);
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpSendLocked);
        }

        await otpStore.SaveSendLimitAsync(
            email,
            sendLimit with { SendCount = sendLimit.SendCount + 1, LastSentAtUtc = now },
            cancellationToken);

        var user = await unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user is not null)
        {
            var code = AuthOtpHelper.GenerateNumericCode();
            await otpStore.SaveOtpAsync(email, _passwordHasher.HashPassword(code), cancellationToken);

            await emailSender.SendAsync(
                email,
                "Wokki — mã xác minh đặt lại mật khẩu",
                $"Mã OTP của bạn là: {code}\n\nMã có hiệu lực 1 phút. Không chia sẻ mã này với ai.",
                cancellationToken);
        }

        return ApiResponse<bool>.SuccessResponse(true, AppMessages.Auth.OtpSent);
    }

    public async Task<ApiResponse<bool>> VerifyForgotPasswordOtpAsync(
        VerifyForgotPasswordOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var challenge = await otpStore.GetActiveOtpAsync(email, cancellationToken);
        if (challenge is null)
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpInvalid);

        if (challenge.AttemptCount >= AuthOtpHelper.MaxVerifyAttempts)
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpInvalid);

        if (!_passwordHasher.VerifyPassword(request.OtpCode.Trim(), challenge.CodeHash))
        {
            await otpStore.UpdateOtpAsync(
                email,
                challenge with { AttemptCount = challenge.AttemptCount + 1 },
                cancellationToken);
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpInvalid);
        }

        await otpStore.MarkVerifiedAsync(email, cancellationToken);
        return ApiResponse<bool>.SuccessResponse(true, AppMessages.Auth.OtpVerified);
    }

    public async Task<ApiResponse<bool>> CompleteForgotPasswordAsync(
        CompleteForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.PasswordConfirmMismatch);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpInvalid);

        var challenge = await otpStore.GetVerifiedOtpAsync(email, cancellationToken);
        if (challenge is null)
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpNotVerified);

        var trackedUser = await unitOfWork.Users.GetByIdAsync(user.Id, track: true, cancellationToken: cancellationToken);
        if (trackedUser is null)
            return ApiResponse<bool>.FailureResponse(AppMessages.Auth.OtpInvalid);

        trackedUser.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        trackedUser.MustChangePassword = false;
        unitOfWork.Users.Update(trackedUser);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await otpStore.DeleteOtpAsync(email, cancellationToken);
        await otpStore.ResetSendLimitAsync(email, cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, AppMessages.Auth.PasswordResetSuccess);
    }

    private async Task<ApiResponse<LoginResponse>> RefreshForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user is null)
            return ApiResponse<LoginResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        var accessFailure = await organizationSubscription.GetAccessFailureAsync(
            user.OrganizationId,
            user.Role,
            cancellationToken);
        if (accessFailure is not null)
            return ApiResponse<LoginResponse>.FailureResponse(accessFailure);

        return ApiResponse<LoginResponse>.SuccessResponse(
            BuildLoginResponse(user),
            AppMessages.Auth.RefreshSuccess);
    }

    public Task<ApiResponse<object>> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _ = userId;
        _ = cancellationToken;
        return Task.FromResult(ApiResponse<object>.SuccessResponse(new { }, AppMessages.Auth.LogoutSuccess));
    }

    public async Task<ApiResponse<UserSimpleResponse>> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user is null)
            return ApiResponse<UserSimpleResponse>.FailureResponse(AppMessages.Auth.Unauthorized);

        return ApiResponse<UserSimpleResponse>.SuccessResponse(ToUserSimple(user), AppMessages.Auth.Me);
    }

    private LoginResponse BuildLoginResponse(UserEntity user) =>
        new(
            jwtTokenService.GenerateAccessToken(user),
            jwtTokenService.GenerateRefreshToken(user),
            user.MustChangePassword);

    private static UserSimpleResponse ToUserSimple(UserEntity user) =>
        new(user.Id, user.Email, user.Role, user.MustChangePassword);
}
