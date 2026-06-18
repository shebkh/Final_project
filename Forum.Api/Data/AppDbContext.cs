// Forum.Api/Data/AppDbContext.cs
using Forum.Api.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Data;

/// <summary>
/// EF Core 9 code-first database context. New feature slices register their
/// entities here and add a migration via `dotnet ef migrations add`.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            // Usernames and emails must be unique across the platform.
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}
