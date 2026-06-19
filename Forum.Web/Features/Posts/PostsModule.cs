// Forum.Web/Features/Posts/PostsModule.cs
using Forum.Web.Features.Auth;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Forum.Web.Features.Posts;

/// <summary>
/// Registers the client-side Posts slice: the typed HttpClient for the API's
/// post (reply) endpoints, wired with the shared AuthTokenHandler so authenticated
/// operations (create/update/delete) carry the bearer token.
/// </summary>
public static class PostsModule
{
    public static IServiceCollection AddPostsFeature(this IServiceCollection services, IConfiguration config)
    {
        var apiBaseAddress = config["ApiBaseAddress"]
            ?? throw new InvalidOperationException("Missing 'ApiBaseAddress' configuration value.");

        // Ensure the bearer handler is available even if this slice is wired
        // without the Auth module. TryAdd is a no-op when Auth already registered it.
        services.TryAddTransient<AuthTokenHandler>();

        services.AddHttpClient<IPostApiClient, PostApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthTokenHandler>();

        return services;
    }
}
