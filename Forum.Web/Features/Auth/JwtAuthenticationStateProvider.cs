// Forum.Web/Features/Auth/JwtAuthenticationStateProvider.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Forum.Web.Features.Auth;

/// <summary>
/// Builds the Blazor authentication state from the stored JWT. Reads claims
/// directly from the token; expired or missing tokens yield an anonymous user.
/// </summary>
public sealed class JwtAuthenticationStateProvider(ITokenStore tokenStore) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly JwtSecurityTokenHandler _handler = new();

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetTokenAsync();
        return BuildState(token);
    }

    /// <summary>Call after a successful login to push the new identity to the UI.</summary>
    public void NotifySignedIn(string token)
    {
        var state = Task.FromResult(BuildState(token));
        NotifyAuthenticationStateChanged(state);
    }

    /// <summary>Call after logout to revert the UI to anonymous.</summary>
    public void NotifySignedOut()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private AuthenticationState BuildState(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !_handler.CanReadToken(token))
            return Anonymous;

        JwtSecurityToken jwt;
        try
        {
            jwt = _handler.ReadJwtToken(token);
        }
        catch
        {
            return Anonymous;
        }

        if (jwt.ValidTo != DateTime.MinValue && jwt.ValidTo < DateTime.UtcNow)
            return Anonymous;

        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt",
            nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
