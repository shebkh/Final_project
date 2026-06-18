// Forum.Api/Features/Auth/IUserRepository.cs
namespace Forum.Api.Features.Auth;

public interface IUserRepository
{
    Task<bool> UserNameExistsAsync(string userName, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>Finds a user by username OR email (case-insensitive). Null if none.</summary>
    Task<User?> FindByUserNameOrEmailAsync(string userNameOrEmail, CancellationToken ct = default);

    Task<User> AddAsync(User user, CancellationToken ct = default);
}
