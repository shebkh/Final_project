// Forum.Api/Data/AppDbContext.cs
using Forum.Api.Features.Auth;
using Forum.Api.Features.Categories;
using Forum.Api.Features.Posts;
using Forum.Api.Features.Threads;
using Forum.Api.Features.Votes;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Data;

/// <summary>
/// EF Core 9 code-first database context. New feature slices register their
/// entities here and add a migration via `dotnet ef migrations add`.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ForumThread> Threads => Set<ForumThread>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<ThreadVote> ThreadVotes => Set<ThreadVote>();
    public DbSet<PostVote> PostVotes => Set<PostVote>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ThreadTag> ThreadTags => Set<ThreadTag>();

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

            // Moderator privilege flag; defaults to false for every new registration.
            entity.Property(u => u.IsModerator)
                .HasDefaultValue(false);

            // Usernames and emails must be unique across the platform.
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<ForumThread>(entity =>
        {
            entity.ToTable("Threads");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(t => t.Body)
                .IsRequired()
                .HasMaxLength(10_000);

            // Moderation flags; default false. Pinned threads sort first (see index below).
            entity.Property(t => t.IsPinned)
                .HasDefaultValue(false);

            entity.Property(t => t.IsLocked)
                .HasDefaultValue(false);

            // A thread belongs to one author; deleting a user is blocked while
            // they still own threads (Restrict) to preserve discussion history.
            entity.HasOne(t => t.Author)
                .WithMany()
                .HasForeignKey(t => t.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // A thread may be filed under one category; deleting the category
            // uncategorizes its threads rather than blocking or cascading.
            entity.HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Listing is ordered newest-first, so index the creation timestamp.
            entity.HasIndex(t => t.CreatedAtUtc);

            // Category filtering on the thread list.
            entity.HasIndex(t => t.CategoryId);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(25);

            // Names are normalized before persistence; uniqueness doubles as the
            // concurrency guard when parallel creates race on a new tag.
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<ThreadTag>(entity =>
        {
            entity.ToTable("ThreadTags");
            entity.HasKey(tt => new { tt.ThreadId, tt.TagId });

            // Join rows die with either side. Cascade is safe here: the paths
            // (Threads → ThreadTags and Tags → ThreadTags) never converge.
            entity.HasOne(tt => tt.Thread)
                .WithMany(t => t.ThreadTags)
                .HasForeignKey(tt => tt.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tt => tt.Tag)
                .WithMany()
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(c => c.Slug)
                .IsRequired()
                .HasMaxLength(100);

            // Slug uniqueness doubles as the duplicate-name guard (insert path
            // catches SQL 2627/2601 and reports a name conflict).
            entity.HasIndex(c => c.Slug).IsUnique();

            // Self-referencing parent FK. SQL Server forbids SET NULL/CASCADE on
            // self-refs, so Restrict; the service blocks deleting a category that
            // still has children.
            entity.HasOne(c => c.Parent)
                .WithMany()
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Body)
                .IsRequired()
                .HasMaxLength(10_000);

            // A post belongs to one thread. Restrict (not Cascade) keeps SQL Server
            // from creating multiple cascade paths (User → Threads → Posts and
            // User → Posts) and preserves history; the service deletes posts explicitly.
            entity.HasOne(p => p.Thread)
                .WithMany()
                .HasForeignKey(p => p.ThreadId)
                .OnDelete(DeleteBehavior.Restrict);

            // A post belongs to one author; blocked from deleting a user who still
            // owns posts, mirroring the Threads → User relationship.
            entity.HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Replies are listed oldest-first within a thread; index the FK + timestamp.
            entity.HasIndex(p => new { p.ThreadId, p.CreatedAtUtc });
        });

        modelBuilder.Entity<ThreadVote>(entity =>
        {
            entity.ToTable("ThreadVotes");
            entity.HasKey(v => v.Id);

            entity.HasOne(v => v.Thread)
                .WithMany()
                .HasForeignKey(v => v.ThreadId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One vote per user per thread.
            entity.HasIndex(v => new { v.ThreadId, v.UserId }).IsUnique();
        });

        modelBuilder.Entity<PostVote>(entity =>
        {
            entity.ToTable("PostVotes");
            entity.HasKey(v => v.Id);

            entity.HasOne(v => v.Post)
                .WithMany()
                .HasForeignKey(v => v.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One vote per user per post.
            entity.HasIndex(v => new { v.PostId, v.UserId }).IsUnique();
        });
    }
}
