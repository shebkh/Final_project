// Forum.Web/Features/Threads/ThreadModels.cs
using System.ComponentModel.DataAnnotations;

namespace Forum.Web.Features.Threads;

// --- Form model (bound by EditForm; the API re-validates with FluentValidation) ---

public sealed class ThreadEditModel
{
    [Required, StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(10_000, MinimumLength = 10)]
    public string Body { get; set; } = string.Empty;

    /// <summary>Optional category; null = uncategorized.</summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Comma-separated tags typed by the author (e.g. "api, ef core, help").
    /// Split into a list by the API client; the API normalizes and re-validates.
    /// </summary>
    [RegularExpression(@"^[A-Za-z0-9 ,-]*$",
        ErrorMessage = "Tags may contain letters, digits, spaces, and hyphens, separated by commas.")]
    [StringLength(160)]
    public string TagsInput { get; set; } = string.Empty;

    /// <summary>TagsInput split on commas, trimmed, empties dropped.</summary>
    public IReadOnlyList<string> ParseTags() =>
        TagsInput.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

// --- Wire DTOs matching the API's request/response shapes ---

public record CreateThreadRequest(string Title, string Body, int? CategoryId, IReadOnlyList<string>? Tags);
public record UpdateThreadRequest(string Title, string Body, int? CategoryId, IReadOnlyList<string>? Tags);

public record ThreadSummaryResponse(
    int Id,
    string Title,
    string Excerpt,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool IsPinned,
    bool IsLocked,
    int? CategoryId,
    string? CategoryName,
    IReadOnlyList<string> Tags);

public record ThreadDetailResponse(
    int Id,
    string Title,
    string Body,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool IsPinned,
    bool IsLocked,
    int? CategoryId,
    string? CategoryName,
    IReadOnlyList<string> Tags);

/// <summary>Result wrapper so components handle failures without exceptions.</summary>
public record ThreadOutcome<T>(bool Succeeded, T? Data, string? Error) where T : class
{
    public static ThreadOutcome<T> Ok(T data) => new(true, data, null);
    public static ThreadOutcome<T> Failed(string error) => new(false, null, error);
}

/// <summary>Result wrapper for operations with no payload (e.g. delete).</summary>
public record ThreadActionOutcome(bool Succeeded, string? Error)
{
    public static ThreadActionOutcome Ok() => new(true, null);
    public static ThreadActionOutcome Failed(string error) => new(false, error);
}
