// Forum.Web/Features/Votes/VoteApiClient.cs
using System.Net;
using System.Net.Http.Json;

namespace Forum.Web.Features.Votes;

public interface IVoteApiClient
{
    Task<VoteOutcome> GetTallyAsync(VoteTargetKind kind, int targetId, CancellationToken ct = default);
    Task<VoteOutcome> CastAsync(VoteTargetKind kind, int targetId, int value, CancellationToken ct = default);
    Task<VoteOutcome> ClearAsync(VoteTargetKind kind, int targetId, CancellationToken ct = default);
}

/// <summary>
/// Typed client for the API's vote endpoints (threads and posts). Injected via
/// IHttpClientFactory (configured in VotesModule). The shared AuthTokenHandler attaches
/// the bearer token, so cast/clear (which require auth) work transparently.
/// </summary>
public sealed class VoteApiClient(HttpClient http) : IVoteApiClient
{
    public Task<VoteOutcome> GetTallyAsync(VoteTargetKind kind, int targetId, CancellationToken ct = default) =>
        SendAsync(() => http.GetAsync(VoteUri(kind, targetId), ct), ct);

    public Task<VoteOutcome> CastAsync(VoteTargetKind kind, int targetId, int value, CancellationToken ct = default) =>
        SendAsync(() => http.PutAsJsonAsync(VoteUri(kind, targetId), new CastVoteRequest(value), ct), ct);

    public Task<VoteOutcome> ClearAsync(VoteTargetKind kind, int targetId, CancellationToken ct = default) =>
        SendAsync(() => http.DeleteAsync(VoteUri(kind, targetId), ct), ct);

    private static string VoteUri(VoteTargetKind kind, int targetId) => kind switch
    {
        VoteTargetKind.Thread => $"api/threads/{targetId}/vote",
        VoteTargetKind.Post => $"api/posts/{targetId}/vote",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static async Task<VoteOutcome> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<VoteTallyResponse>(cancellationToken: ct);
                return data is not null
                    ? VoteOutcome.Ok(data)
                    : VoteOutcome.Failed("The server returned an empty response.");
            }

            return VoteOutcome.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return VoteOutcome.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return VoteOutcome.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return "You need to sign in to vote.";
            case HttpStatusCode.NotFound:
                return "That item no longer exists.";
            case HttpStatusCode.BadRequest:
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken: ct);
                var firstError = problem?.Errors?.Values.SelectMany(v => v).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstError))
                    return firstError;
                break;
        }

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken: ct);
            if (!string.IsNullOrWhiteSpace(error?.Error))
                return error.Error;
        }
        catch
        {
            // Not a JSON error body — fall through to a generic message.
        }

        return $"Request failed ({(int)response.StatusCode}).";
    }

    private record ApiError(string? Error);
    private record ValidationProblem(Dictionary<string, string[]>? Errors);
}
