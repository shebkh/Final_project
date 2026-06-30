// Forum.Web/Features/Auth/AuthModels.cs
using System.ComponentModel.DataAnnotations;

namespace Forum.Web.Features.Auth;

// --- Form models (bound by EditForm; DataAnnotations give client-side UX;
//     the API re-validates authoritatively with FluentValidation) ---

public sealed class RegisterModel
{
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Letters, numbers, and underscores only.")]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginModel
{
    [Required]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// --- Wire DTOs matching the API's request/response shapes ---

public record RegisterRequest(string UserName, string Email, string Password);
public record LoginRequest(string UserNameOrEmail, string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    int UserId,
    string UserName,
    string Email,
    bool IsModerator);

/// <summary>Result wrapper returned to components so they can show errors without exceptions.</summary>
public record AuthOutcome(bool Succeeded, AuthResponse? Data, string? Error)
{
    public static AuthOutcome Ok(AuthResponse data) => new(true, data, null);
    public static AuthOutcome Failed(string error) => new(false, null, error);
}
