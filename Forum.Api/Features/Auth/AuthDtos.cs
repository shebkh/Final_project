// Forum.Api/Features/Auth/AuthDtos.cs
namespace Forum.Api.Features.Auth;

/// <summary>Request body for POST /api/auth/register.</summary>
public record RegisterRequest(string UserName, string Email, string Password);

/// <summary>Request body for POST /api/auth/login.</summary>
public record LoginRequest(string UserNameOrEmail, string Password);

/// <summary>
/// Returned on successful register/login. Carries the JWT plus the
/// minimal user info the client needs to render the signed-in state.
/// </summary>
public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    int UserId,
    string UserName,
    string Email);
