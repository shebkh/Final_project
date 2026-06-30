// Forum.Api/Features/Auth/User.cs
namespace Forum.Api.Features.Auth;

/// <summary>
/// EF Core entity representing a registered forum user.
/// Never exposed directly over the API — always projected to a DTO.
/// </summary>
public class User
{
    public int Id { get; set; }

    public required string UserName { get; set; }

    public required string Email { get; set; }

    /// <summary>BCrypt hash of the user's password. Never returned to clients.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Whether this user has moderator privileges (pin/lock threads, delete any content).
    /// Seeded manually in the database; a user must re-login after promotion so the new
    /// claim is issued into their JWT. Defaults to false for all registrations.
    /// </summary>
    public bool IsModerator { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
