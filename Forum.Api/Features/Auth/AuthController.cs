// Forum.Api/Features/Auth/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return MapResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return MapResult(result);
    }

    private IActionResult MapResult(AuthResult result) => result.Error switch
    {
        AuthError.None when result.Response is not null => Ok(result.Response),
        AuthError.UserNameTaken => Conflict(new { error = "That username is already taken." }),
        AuthError.EmailTaken => Conflict(new { error = "That email is already registered." }),
        AuthError.InvalidCredentials => Unauthorized(new { error = "Invalid username or password." }),
        _ => Problem("An unexpected error occurred.")
    };
}
