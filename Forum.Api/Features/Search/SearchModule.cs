// Forum.Api/Features/Search/SearchModule.cs
namespace Forum.Api.Features.Search;

/// <summary>
/// Registers all services that belong to the Search vertical slice.
/// Called from Program.cs so feature wiring stays inside the feature folder.
/// </summary>
public static class SearchModule
{
    public static IServiceCollection AddSearchFeature(this IServiceCollection services)
    {
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<ISearchService, SearchService>();

        return services;
    }
}
