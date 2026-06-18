// Forum.Api/Features/Auth/IJwtTokenService.cs
namespace Forum.Api.Features.Auth;

public interface IJwtTokenService
{
    /// <summary>Issues a signed JWT for the given user and returns the token plus its UTC expiry.</summary>
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
