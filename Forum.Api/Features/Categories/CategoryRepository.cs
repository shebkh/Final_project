// Forum.Api/Features/Categories/CategoryRepository.cs
using Forum.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Categories;

public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, int>> ThreadCountsAsync(CancellationToken ct = default) =>
        await db.Threads
            .AsNoTracking()
            .Where(t => t.CategoryId != null)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    // Tracked, no Include — so SaveChanges writes only the Categories row and never
    // traverses into the Parent navigation.
    public Task<Category?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        db.Categories.AnyAsync(c => c.Id == id, ct);

    public Task<bool> HasChildrenAsync(int id, CancellationToken ct = default) =>
        db.Categories.AnyAsync(c => c.ParentId == id, ct);

    public async Task<bool> TryAddAsync(Category category, CancellationToken ct = default)
    {
        db.Categories.Add(category);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A category with the same slug already exists. Detach our failed
            // insert so the context stays usable and report the conflict.
            db.Entry(category).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TrySaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
    }

    public async Task DeleteAsync(Category category, CancellationToken ct = default)
    {
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    // SQL Server: 2627 = unique constraint violation, 2601 = duplicate key in a unique index.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2627 || sql.Number == 2601);
}
