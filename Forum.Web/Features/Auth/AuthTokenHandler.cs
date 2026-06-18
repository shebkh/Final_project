// Forum.Web/Features/Auth/AuthTokenHandler.cs
using System.Net.Http.Headers;

namespace Forum.Web.Features.Auth;

/// <summary>
/// Attaches the stored JWT as a Bearer token on every outgoing request made
/// through the typed API clients. Registered on the HttpClient via AddHttpMessageHandler.
/// </summary>
public sealed class AuthTokenHandler(ITokenStore tokenStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenStore.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
