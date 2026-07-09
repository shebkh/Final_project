// Forum.Api/Features/Threads/ThreadDtos.cs
namespace Forum.Api.Features.Threads;

/// <summary>Request body for POST /api/threads. CategoryId and Tags are optional.</summary>
public record CreateThreadRequest(string Title, string Body, int? CategoryId, IReadOnlyList<string>? Tags);

/// <summary>Request body for PUT /api/threads/{id}. CategoryId and Tags are optional; Tags replace the existing set.</summary>
public record UpdateThreadRequest(string Title, string Body, int? CategoryId, IReadOnlyList<string>? Tags);

/// <summary>Lightweight projection for the thread list (no full body).</summary>
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

/// <summary>Full projection for a single thread's detail view.</summary>
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
