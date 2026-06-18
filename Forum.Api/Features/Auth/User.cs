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

    public DateTime CreatedAtUtc { get; set; }
}
