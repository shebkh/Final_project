// Forum.Api/Features/Auth/UserRepository.cs
using Forum.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Auth;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> UserNameExistsAsync(string userName, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.UserName == userName, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email == email, ct);

    public Task<User?> FindByUserNameOrEmailAsync(string userNameOrEmail, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(
            u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
