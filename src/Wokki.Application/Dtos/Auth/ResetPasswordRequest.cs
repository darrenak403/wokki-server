namespace Wokki.Application.Dtos.Auth;

/// <summary>Đổi mật khẩu khi đã đăng nhập — mật khẩu cũ + mới + xác nhận.</summary>
public sealed record ResetPasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
