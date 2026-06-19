// Forum.Api/Features/Threads/ThreadDtos.cs
namespace Forum.Api.Features.Threads;

/// <summary>Request body for POST /api/threads.</summary>
public record CreateThreadRequest(string Title, string Body);

/// <summary>Request body for PUT /api/threads/{id}.</summary>
public record UpdateThreadRequest(string Title, string Body);

/// <summary>Lightweight projection for the thread list (no full body).</summary>
public record ThreadSummaryResponse(
    int Id,
    string Title,
    string Excerpt,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>Full projection for a single thread's detail view.</summary>
public record ThreadDetailResponse(
    int Id,
    string Title,
    string Body,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
