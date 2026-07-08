// Forum.Web/Features/Search/SearchModule.cs
using Forum.Web.Features.Auth;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Forum.Web.Features.Search;

/// <summary>
/// Registers the client-side Search slice: the typed HttpClient for the API's
/// search endpoint. Search is anonymous, but the shared AuthTokenHandler is
/// attached anyway so the slice matches the standard template.
/// </summary>
public static class SearchModule
{
    public static IServiceCollection AddSearchFeature(this IServiceCollection services, IConfiguration config)
    {
        var apiBaseAddress = config["ApiBaseAddress"]
            ?? throw new InvalidOperationException("Missing 'ApiBaseAddress' configuration value.");

        // Ensure the bearer handler is available even if this slice is wired
        // without the Auth module. TryAdd is a no-op when Auth already registered it.
        services.TryAddTransient<AuthTokenHandler>();

        services.AddHttpClient<ISearchApiClient, SearchApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthTokenHandler>();

        return services;
    }
}
