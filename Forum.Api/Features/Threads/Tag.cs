// Forum.Api/Features/Threads/Tag.cs
namespace Forum.Api.Features.Threads;

/// <summary>
/// EF Core entity for a free-form thread tag. Names are normalized by the
/// service (trimmed, lowercase, spaces hyphenated) before persistence, and the
/// unique index on Name doubles as the concurrency guard for parallel creates.
/// Never exposed directly over the API — threads project tags as plain strings.
/// </summary>
public class Tag
{
    public int Id { get; set; }

    public required string Name { get; set; }
}
