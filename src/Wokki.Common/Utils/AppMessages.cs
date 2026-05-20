using Microsoft.AspNetCore.Http;

namespace Wokki.Common.Utils;

public static class AppMessages
{
    public static class Health
    {
        public static readonly AppMessage Ok = new("HEALTH_OK", "Service is healthy.", StatusCodes.Status200OK);
    }

    public static class User
    {
        public static readonly AppMessage Found = new("USER_FOUND", "User found.", StatusCodes.Status200OK);
        public static readonly AppMessage Listed = new("USER_LISTED", "Users listed.", StatusCodes.Status200OK);
        public static readonly AppMessage NotFound = new("USER_NOT_FOUND", "User not found.", StatusCodes.Status404NotFound);
        public static readonly AppMessage Created = new("USER_CREATED", "User created.", StatusCodes.Status201Created);
        public static readonly AppMessage Exists = new("USER_EXISTS", "Email already registered.", StatusCodes.Status409Conflict);
    }

    public static class Auth
    {
        public static readonly AppMessage LoginSuccess = new("AUTH_LOGIN_SUCCESS", "Login successful.", StatusCodes.Status200OK);
        public static readonly AppMessage RefreshSuccess = new("AUTH_REFRESH_SUCCESS", "Token refreshed.", StatusCodes.Status200OK);
        public static readonly AppMessage Unauthorized = new("AUTH_UNAUTHORIZED", "Unauthorized.", StatusCodes.Status401Unauthorized);
        public static readonly AppMessage Forbidden = new("AUTH_FORBIDDEN", "Forbidden.", StatusCodes.Status403Forbidden);
        public static readonly AppMessage InvalidCredentials = new("AUTH_INVALID_CREDENTIALS", "Invalid credentials.", StatusCodes.Status401Unauthorized);
        public static readonly AppMessage Me = new("AUTH_ME", "Current user profile.", StatusCodes.Status200OK);
        public static readonly AppMessage LogoutSuccess = new("AUTH_LOGOUT_SUCCESS", "Logged out.", StatusCodes.Status200OK);
        public static readonly AppMessage NotLoggedIn = new("AUTH_NOT_LOGGED_IN", "Not authenticated.", StatusCodes.Status401Unauthorized);
    }

    public static class Validation
    {
        public static readonly AppMessage Failed = new("VALIDATION_FAILED", "Validation failed.", StatusCodes.Status400BadRequest);
    }

    public static class Internal
    {
        public static readonly AppMessage Error = new("INTERNAL_ERROR", "An unexpected error occurred.", StatusCodes.Status500InternalServerError);
    }
}
