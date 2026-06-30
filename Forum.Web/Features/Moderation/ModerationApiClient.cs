// Forum.Web/Features/Moderation/ModerationApiClient.cs
using System.Net;
using System.Net.Http.Json;

namespace Forum.Web.Features.Moderation;

public interface IModerationApiClient
{
    Task<ModerationOutcome> SetPinnedAsync(int threadId, bool pinned, CancellationToken ct = default);
    Task<ModerationOutcome> SetLockedAsync(int threadId, bool locked, CancellationToken ct = default);
}

/// <summary>
/// Typed client for the API's moderation endpoints. Injected via IHttpClientFactory
/// (configured in ModerationModule). The shared AuthTokenHandler attaches the bearer
/// token, so the moderator-role-gated endpoints authorize correctly.
/// </summary>
public sealed class ModerationApiClient(HttpClient http) : IModerationApiClient
{
    public Task<ModerationOutcome> SetPinnedAsync(int threadId, bool pinned, CancellationToken ct = default) =>
        SendAsync(() => http.PutAsJsonAsync(
            $"api/moderation/threads/{threadId}/pin", new SetPinRequest(pinned), ct), ct);

    public Task<ModerationOutcome> SetLockedAsync(int threadId, bool locked, CancellationToken ct = default) =>
        SendAsync(() => http.PutAsJsonAsync(
            $"api/moderation/threads/{threadId}/lock", new SetLockRequest(locked), ct), ct);

    private static async Task<ModerationOutcome> SendAsync(
        Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<ThreadModerationResponse>(cancellationToken: ct);
                return data is not null
                    ? ModerationOutcome.Ok(data)
                    : ModerationOutcome.Failed("The server returned an empty response.");
            }

            return ModerationOutcome.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return ModerationOutcome.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return ModerationOutcome.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return "You need to sign in to do that.";
            case HttpStatusCode.Forbidden:
                return "Only moderators can do that.";
            case HttpStatusCode.NotFound:
                return "That thread no longer exists.";
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
}
