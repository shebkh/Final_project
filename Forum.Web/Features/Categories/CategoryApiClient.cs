// Forum.Web/Features/Categories/CategoryApiClient.cs
using System.Net;
using System.Net.Http.Json;

namespace Forum.Web.Features.Categories;

public interface ICategoryApiClient
{
    Task<CategoryOutcome<IReadOnlyList<CategoryResponse>>> ListAsync(CancellationToken ct = default);

    Task<CategoryOutcome<CategoryResponse>> CreateAsync(CategoryEditModel model, CancellationToken ct = default);

    Task<CategoryOutcome<CategoryResponse>> UpdateAsync(int id, CategoryEditModel model, CancellationToken ct = default);

    Task<CategoryActionOutcome> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Typed client for the API's category endpoints. Injected via IHttpClientFactory
/// (configured in CategoriesModule). The shared AuthTokenHandler attaches the bearer
/// token, so moderator operations (create/update/delete) work transparently.
/// </summary>
public sealed class CategoryApiClient(HttpClient http) : ICategoryApiClient
{
    public async Task<CategoryOutcome<IReadOnlyList<CategoryResponse>>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync("api/categories", ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content
                    .ReadFromJsonAsync<List<CategoryResponse>>(cancellationToken: ct);
                return CategoryOutcome<IReadOnlyList<CategoryResponse>>.Ok(data ?? []);
            }

            return CategoryOutcome<IReadOnlyList<CategoryResponse>>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return CategoryOutcome<IReadOnlyList<CategoryResponse>>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return CategoryOutcome<IReadOnlyList<CategoryResponse>>.Failed("The request timed out. Please try again.");
        }
    }

    public Task<CategoryOutcome<CategoryResponse>> CreateAsync(CategoryEditModel model, CancellationToken ct = default) =>
        SendAsync(() => http.PostAsJsonAsync("api/categories",
            new CreateCategoryRequest(model.Name, model.ParentId), ct), ct);

    public Task<CategoryOutcome<CategoryResponse>> UpdateAsync(int id, CategoryEditModel model, CancellationToken ct = default) =>
        SendAsync(() => http.PutAsJsonAsync($"api/categories/{id}",
            new UpdateCategoryRequest(model.Name, model.ParentId), ct), ct);

    public async Task<CategoryActionOutcome> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.DeleteAsync($"api/categories/{id}", ct);
            return response.IsSuccessStatusCode
                ? CategoryActionOutcome.Ok()
                : CategoryActionOutcome.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return CategoryActionOutcome.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return CategoryActionOutcome.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<CategoryOutcome<CategoryResponse>> SendAsync(
        Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CategoryResponse>(cancellationToken: ct);
                return data is not null
                    ? CategoryOutcome<CategoryResponse>.Ok(data)
                    : CategoryOutcome<CategoryResponse>.Failed("The server returned an empty response.");
            }

            return CategoryOutcome<CategoryResponse>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return CategoryOutcome<CategoryResponse>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return CategoryOutcome<CategoryResponse>.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return "You need to sign in to do that.";
            case HttpStatusCode.Forbidden:
                return "Only moderators can manage categories.";
            case HttpStatusCode.NotFound:
                return "That category no longer exists.";
            case HttpStatusCode.BadRequest:
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken: ct);
                var firstError = problem?.Errors?.Values.SelectMany(v => v).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstError))
                    return firstError;
                break;
        }

        // Conflict (409) and error-shaped 400s carry an { error } body.
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
