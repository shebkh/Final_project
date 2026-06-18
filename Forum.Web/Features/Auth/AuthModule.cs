// Forum.Web/Features/Auth/AuthModule.cs
using Microsoft.AspNetCore.Components.Authorization;

namespace Forum.Web.Features.Auth;

/// <summary>
/// Registers the client-side Auth slice: token store, auth state provider,
/// orchestration manager, and the typed HttpClient (with bearer handler).
/// </summary>
public static class AuthModule
{
    public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration config)
    {
        var apiBaseAddress = config["ApiBaseAddress"]
            ?? throw new InvalidOperationException("Missing 'ApiBaseAddress' configuration value.");

        services.AddScoped<ITokenStore, TokenStore>();
        services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
        services.AddScoped<IAuthManager, AuthManager>();

        // DelegatingHandler must be registered as transient for the factory.
        services.AddTransient<AuthTokenHandler>();

        services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthTokenHandler>();

        return services;
    }
}
