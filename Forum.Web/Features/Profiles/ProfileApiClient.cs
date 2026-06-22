// Forum.Web/Features/Profiles/ProfileApiClient.cs
using System.Net;
using System.Net.Http.Json;

namespace Forum.Web.Features.Profiles;

public interface IProfileApiClient
{
    Task<ProfileOutcome<UserProfileResponse>> GetProfileAsync(int id, CancellationToken ct = default);

    Task<ProfileOutcome<IReadOnlyList<ProfileThreadResponse>>> ListThreadsAsync(
        int id, int page = 1, int pageSize = 20, CancellationToken ct = default);

    Task<ProfileOutcome<IReadOnlyList<ProfilePostResponse>>> ListPostsAsync(
        int id, int page = 1, int pageSize = 20, CancellationToken ct = default);
}

/// <summary>
/// Typed client for the API's public user-profile endpoints. Injected via
/// IHttpClientFactory (configured in ProfilesModule). Profiles are anonymous
/// reads, but the shared AuthTokenHandler is harmless when no token is present.
/// </summary>
public sealed class ProfileApiClient(HttpClient http) : IProfileApiClient
{
    public Task<ProfileOutcome<UserProfileResponse>> GetProfileAsync(int id, CancellationToken ct = default) =>
        SendForDataAsync<UserProfileResponse>(() => http.GetAsync($"api/users/{id}", ct), ct);

    public Task<ProfileOutcome<IReadOnlyList<ProfileThreadResponse>>> ListThreadsAsync(
        int id, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        SendForListAsync<ProfileThreadResponse>(
            () => http.GetAsync($"api/users/{id}/threads?page={page}&pageSize={pageSize}", ct), ct);

    public Task<ProfileOutcome<IReadOnlyList<ProfilePostResponse>>> ListPostsAsync(
        int id, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        SendForListAsync<ProfilePostResponse>(
            () => http.GetAsync($"api/users/{id}/posts?page={page}&pageSize={pageSize}", ct), ct);

    private static async Task<ProfileOutcome<T>> SendForDataAsync<T>(
        Func<Task<HttpResponseMessage>> send, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await send();
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return data is not null
                    ? ProfileOutcome<T>.Ok(data)
                    : ProfileOutcome<T>.Failed("The server returned an empty response.");
            }

            return ProfileOutcome<T>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return ProfileOutcome<T>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return ProfileOutcome<T>.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<ProfileOutcome<IReadOnlyList<T>>> SendForListAsync<T>(
        Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct);
                return ProfileOutcome<IReadOnlyList<T>>.Ok(data ?? []);
            }

            return ProfileOutcome<IReadOnlyList<T>>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return ProfileOutcome<IReadOnlyList<T>>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return ProfileOutcome<IReadOnlyList<T>>.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
            return "That user could not be found.";

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
