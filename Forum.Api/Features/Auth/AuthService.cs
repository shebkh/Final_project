// Forum.Api/Features/Auth/AuthService.cs
namespace Forum.Api.Features.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    IJwtTokenService tokenService) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await userRepository.UserNameExistsAsync(userName, ct))
            return AuthResult.Fail(AuthError.UserNameTaken);

        if (await userRepository.EmailExistsAsync(email, ct))
            return AuthResult.Fail(AuthError.EmailTaken);

        var user = new User
        {
            UserName = userName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAtUtc = DateTime.UtcNow
        };

        await userRepository.AddAsync(user, ct);

        return AuthResult.Success(BuildResponse(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var identifier = request.UserNameOrEmail.Trim();
        var user = await userRepository.FindByUserNameOrEmailAsync(identifier, ct);

        // Verify even when the user is missing is unnecessary here; a single
        // generic error avoids leaking which accounts exist.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return AuthResult.Fail(AuthError.InvalidCredentials);

        return AuthResult.Success(BuildResponse(user));
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expiresAtUtc) = tokenService.CreateToken(user);
        return new AuthResponse(token, expiresAtUtc, user.Id, user.UserName, user.Email);
    }
}
