// Forum.Api/Features/Threads/ThreadsModule.cs
using FluentValidation;

namespace Forum.Api.Features.Threads;

/// <summary>
/// Registers all services that belong to the Threads vertical slice.
/// Called from Program.cs so feature wiring stays inside the feature folder.
/// </summary>
public static class ThreadsModule
{
    public static IServiceCollection AddThreadsFeature(this IServiceCollection services)
    {
        services.AddScoped<IThreadRepository, ThreadRepository>();
        services.AddScoped<IThreadService, ThreadService>();

        services.AddScoped<IValidator<CreateThreadRequest>, CreateThreadRequestValidator>();
        services.AddScoped<IValidator<UpdateThreadRequest>, UpdateThreadRequestValidator>();

        return services;
    }
}
