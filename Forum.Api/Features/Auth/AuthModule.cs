// Forum.Api/Features/Auth/AuthModule.cs
using FluentValidation;

namespace Forum.Api.Features.Auth;

/// <summary>
/// Registers all services that belong to the Auth vertical slice.
/// Called from Program.cs so feature wiring stays inside the feature folder.
/// </summary>
public static class AuthModule
{
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

        return services;
    }
}
