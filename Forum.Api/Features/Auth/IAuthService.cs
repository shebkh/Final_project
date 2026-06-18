// Forum.Api/Features/Auth/IAuthService.cs
namespace Forum.Api.Features.Auth;

/// <summary>
/// Outcome of an auth operation. Keeps HTTP-mapping decisions out of the
/// service while letting the controller translate to the right status code.
/// </summary>
public enum AuthError
{
    None = 0,
    UserNameTaken,
    EmailTaken,
    InvalidCredentials
}

public readonly record struct AuthResult(AuthResponse? Response, AuthError Error)
{
    public bool Succeeded => Error == AuthError.None && Response is not null;

    public static AuthResult Success(AuthResponse response) => new(response, AuthError.None);
    public static AuthResult Fail(AuthError error) => new(null, error);
}

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
