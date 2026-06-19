// Forum.Web/Features/Threads/ThreadApiClient.cs
using System.Net;
using System.Net.Http.Json;

namespace Forum.Web.Features.Threads;

public interface IThreadApiClient
{
    Task<ThreadOutcome<IReadOnlyList<ThreadSummaryResponse>>> ListAsync(
        int page = 1, int pageSize = 20, CancellationToken ct = default);

    Task<ThreadOutcome<ThreadDetailResponse>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ThreadOutcome<ThreadDetailResponse>> CreateAsync(ThreadEditModel model, CancellationToken ct = default);

    Task<ThreadOutcome<ThreadDetailResponse>> UpdateAsync(int id, ThreadEditModel model, CancellationToken ct = default);

    Task<ThreadActionOutcome> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Typed client for the API's thread endpoints. Injected via IHttpClientFactory
/// (configured in ThreadsModule). The shared AuthTokenHandler attaches the bearer
/// token, so authenticated calls (create/update/delete) work transparently.
/// </summary>
public sealed class ThreadApiClient(HttpClient http) : IThreadApiClient
{
    public async Task<ThreadOutcome<IReadOnlyList<ThreadSummaryResponse>>> ListAsync(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync($"api/threads?page={page}&pageSize={pageSize}", ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<ThreadSummaryResponse>>(cancellationToken: ct);
                return ThreadOutcome<IReadOnlyList<ThreadSummaryResponse>>.Ok(data ?? []);
            }

            return ThreadOutcome<IReadOnlyList<ThreadSummaryResponse>>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return ThreadOutcome<IReadOnlyList<ThreadSummaryResponse>>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return ThreadOutcome<IReadOnlyList<ThreadSummaryResponse>>.Failed("The request timed out. Please try again.");
        }
    }

    public Task<ThreadOutcome<ThreadDetailResponse>> GetByIdAsync(int id, CancellationToken ct = default) =>
        SendForDetailAsync(() => http.GetAsync($"api/threads/{id}", ct), ct);

    public Task<ThreadOutcome<ThreadDetailResponse>> CreateAsync(ThreadEditModel model, CancellationToken ct = default) =>
        SendForDetailAsync(() => http.PostAsJsonAsync("api/threads",
            new CreateThreadRequest(model.Title, model.Body), ct), ct);

    public Task<ThreadOutcome<ThreadDetailResponse>> UpdateAsync(int id, ThreadEditModel model, CancellationToken ct = default) =>
        SendForDetailAsync(() => http.PutAsJsonAsync($"api/threads/{id}",
            new UpdateThreadRequest(model.Title, model.Body), ct), ct);

    public async Task<ThreadActionOutcome> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.DeleteAsync($"api/threads/{id}", ct);
            return response.IsSuccessStatusCode
                ? ThreadActionOutcome.Ok()
                : ThreadActionOutcome.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return ThreadActionOutcome.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return ThreadActionOutcome.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<ThreadOutcome<ThreadDetailResponse>> SendForDetailAsync(
        Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<ThreadDetailResponse>(cancellationToken: ct);
                return data is not null
                    ? ThreadOutcome<ThreadDetailResponse>.Ok(data)
                    : ThreadOutcome<ThreadDetailResponse>.Failed("The server returned an empty response.");
            }

            return ThreadOutcome<ThreadDetailResponse>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return ThreadOutcome<ThreadDetailResponse>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return ThreadOutcome<ThreadDetailResponse>.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return "You need to sign in to do that.";
            case HttpStatusCode.Forbidden:
                return "You can only modify your own threads.";
            case HttpStatusCode.NotFound:
                return "That thread no longer exists.";
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
