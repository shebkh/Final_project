// Forum.Api/Features/Posts/PostsModule.cs
using FluentValidation;

namespace Forum.Api.Features.Posts;

/// <summary>
/// Registers all services that belong to the Posts vertical slice.
/// Called from Program.cs so feature wiring stays inside the feature folder.
/// </summary>
public static class PostsModule
{
    public static IServiceCollection AddPostsFeature(this IServiceCollection services)
    {
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IPostService, PostService>();

        services.AddScoped<IValidator<CreatePostRequest>, CreatePostRequestValidator>();
        services.AddScoped<IValidator<UpdatePostRequest>, UpdatePostRequestValidator>();

        return services;
    }
}
