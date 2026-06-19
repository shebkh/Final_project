// Forum.Web/Features/Posts/PostApiClient.cs
using System.Net;
using System.Net.Http.Json;

namespace Forum.Web.Features.Posts;

public interface IPostApiClient
{
    Task<PostOutcome<IReadOnlyList<PostResponse>>> ListByThreadAsync(
        int threadId, int page = 1, int pageSize = 50, CancellationToken ct = default);

    Task<PostOutcome<PostResponse>> CreateAsync(int threadId, PostEditModel model, CancellationToken ct = default);

    Task<PostOutcome<PostResponse>> UpdateAsync(int id, PostEditModel model, CancellationToken ct = default);

    Task<PostActionOutcome> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Typed client for the API's post (reply) endpoints. Injected via IHttpClientFactory
/// (configured in PostsModule). The shared AuthTokenHandler attaches the bearer token,
/// so authenticated calls (create/update/delete) work transparently.
/// </summary>
public sealed class PostApiClient(HttpClient http) : IPostApiClient
{
    public async Task<PostOutcome<IReadOnlyList<PostResponse>>> ListByThreadAsync(
        int threadId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync(
                $"api/threads/{threadId}/posts?page={page}&pageSize={pageSize}", ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<PostResponse>>(cancellationToken: ct);
                return PostOutcome<IReadOnlyList<PostResponse>>.Ok(data ?? []);
            }

            return PostOutcome<IReadOnlyList<PostResponse>>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return PostOutcome<IReadOnlyList<PostResponse>>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return PostOutcome<IReadOnlyList<PostResponse>>.Failed("The request timed out. Please try again.");
        }
    }

    public Task<PostOutcome<PostResponse>> CreateAsync(int threadId, PostEditModel model, CancellationToken ct = default) =>
        SendForPostAsync(() => http.PostAsJsonAsync($"api/threads/{threadId}/posts",
            new CreatePostRequest(model.Body), ct), ct);

    public Task<PostOutcome<PostResponse>> UpdateAsync(int id, PostEditModel model, CancellationToken ct = default) =>
        SendForPostAsync(() => http.PutAsJsonAsync($"api/posts/{id}",
            new UpdatePostRequest(model.Body), ct), ct);

    public async Task<PostActionOutcome> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.DeleteAsync($"api/posts/{id}", ct);
            return response.IsSuccessStatusCode
                ? PostActionOutcome.Ok()
                : PostActionOutcome.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return PostActionOutcome.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return PostActionOutcome.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<PostOutcome<PostResponse>> SendForPostAsync(
        Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: ct);
                return data is not null
                    ? PostOutcome<PostResponse>.Ok(data)
                    : PostOutcome<PostResponse>.Failed("The server returned an empty response.");
            }

            return PostOutcome<PostResponse>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return PostOutcome<PostResponse>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return PostOutcome<PostResponse>.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return "You need to sign in to do that.";
            case HttpStatusCode.Forbidden:
                return "You can only modify your own replies.";
            case HttpStatusCode.NotFound:
                return "That thread or reply no longer exists.";
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
