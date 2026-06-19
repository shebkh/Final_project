// Forum.Web/Features/Posts/PostModels.cs
using System.ComponentModel.DataAnnotations;

namespace Forum.Web.Features.Posts;

// --- Form model (bound by EditForm; the API re-validates with FluentValidation) ---

public sealed class PostEditModel
{
    [Required, StringLength(10_000, MinimumLength = 2)]
    public string Body { get; set; } = string.Empty;
}

// --- Wire DTOs matching the API's request/response shapes ---

public record CreatePostRequest(string Body);
public record UpdatePostRequest(string Body);

public record PostResponse(
    int Id,
    int ThreadId,
    string Body,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>Result wrapper so components handle failures without exceptions.</summary>
public record PostOutcome<T>(bool Succeeded, T? Data, string? Error) where T : class
{
    public static PostOutcome<T> Ok(T data) => new(true, data, null);
    public static PostOutcome<T> Failed(string error) => new(false, null, error);
}

/// <summary>Result wrapper for operations with no payload (e.g. delete).</summary>
public record PostActionOutcome(bool Succeeded, string? Error)
{
    public static PostActionOutcome Ok() => new(true, null);
    public static PostActionOutcome Failed(string error) => new(false, error);
}
