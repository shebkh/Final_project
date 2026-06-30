// Forum.Api/Features/Threads/IThreadService.cs
namespace Forum.Api.Features.Threads;

/// <summary>
/// Outcome classification for thread operations. Keeps HTTP-status decisions
/// in the controller while the service expresses domain results.
/// </summary>
public enum ThreadError
{
    None = 0,
    NotFound,
    Forbidden
}

/// <summary>Generic result carrying an optional payload plus an error classification.</summary>
public readonly record struct ThreadResult<T>(T? Value, ThreadError Error) where T : class
{
    public bool Succeeded => Error == ThreadError.None;

    public static ThreadResult<T> Success(T value) => new(value, ThreadError.None);
    public static ThreadResult<T> Fail(ThreadError error) => new(null, error);
}

public interface IThreadService
{
    Task<IReadOnlyList<ThreadSummaryResponse>> ListAsync(int page, int pageSize, CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);

    Task<ThreadResult<ThreadDetailResponse>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ThreadDetailResponse> CreateAsync(CreateThreadRequest request, int authorId, CancellationToken ct = default);

    Task<ThreadResult<ThreadDetailResponse>> UpdateAsync(
        int id, UpdateThreadRequest request, int currentUserId, CancellationToken ct = default);

    Task<ThreadResult<ThreadDetailResponse>> DeleteAsync(
        int id, int currentUserId, bool isModerator, CancellationToken ct = default);
}
