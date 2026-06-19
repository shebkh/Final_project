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
}

// --- Wire DTOs matching the API's request/response shapes ---

public record CreateThreadRequest(string Title, string Body);
public record UpdateThreadRequest(string Title, string Body);

public record ThreadSummaryResponse(
    int Id,
    string Title,
    string Excerpt,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record ThreadDetailResponse(
    int Id,
    string Title,
    string Body,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

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
