// Forum.Api/Features/Auth/JwtOptions.cs
namespace Forum.Api.Features.Auth;

/// <summary>
/// Strongly-typed binding of the "Jwt" section in appsettings.json.
/// Bound via the options pattern in Program.cs.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; init; } = 120;
}
