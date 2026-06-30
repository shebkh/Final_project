// Forum.Api/Features/Auth/ModeratorRole.cs
namespace Forum.Api.Features.Auth;

/// <summary>
/// The single moderator role used across the platform. Centralised so the JWT claim,
/// [Authorize(Roles = ...)] attributes, and any policy all reference the same string.
/// </summary>
public static class ModeratorRole
{
    public const string Name = "Moderator";
}
