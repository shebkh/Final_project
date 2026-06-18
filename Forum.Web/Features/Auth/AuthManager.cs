// Forum.Web/Features/Auth/AuthManager.cs
using Microsoft.AspNetCore.Components.Authorization;

namespace Forum.Web.Features.Auth;

public interface IAuthManager
{
    Task<AuthOutcome> RegisterAsync(RegisterModel model, CancellationToken ct = default);
    Task<AuthOutcome> LoginAsync(LoginModel model, CancellationToken ct = default);
    Task LogoutAsync();
}

/// <summary>
/// Orchestrates the login/register/logout flow for components: calls the API,
/// persists the token, and notifies the authentication state provider so the
/// whole UI reacts. Components depend on this, not on the lower-level pieces.
/// </summary>
public sealed class AuthManager(
    IAuthApiClient apiClient,
    ITokenStore tokenStore,
    AuthenticationStateProvider authStateProvider) : IAuthManager
{
    public async Task<AuthOutcome> RegisterAsync(RegisterModel model, CancellationToken ct = default)
    {
        var outcome = await apiClient.RegisterAsync(model, ct);
        if (outcome.Succeeded)
            await SignInAsync(outcome.Data!);
        return outcome;
    }

    public async Task<AuthOutcome> LoginAsync(LoginModel model, CancellationToken ct = default)
    {
        var outcome = await apiClient.LoginAsync(model, ct);
        if (outcome.Succeeded)
            await SignInAsync(outcome.Data!);
        return outcome;
    }

    public async Task LogoutAsync()
    {
        await tokenStore.ClearTokenAsync();
        AsJwtProvider().NotifySignedOut();
    }

    private async Task SignInAsync(AuthResponse data)
    {
        await tokenStore.SetTokenAsync(data.Token);
        AsJwtProvider().NotifySignedIn(data.Token);
    }

    private JwtAuthenticationStateProvider AsJwtProvider() =>
        (JwtAuthenticationStateProvider)authStateProvider;
}
