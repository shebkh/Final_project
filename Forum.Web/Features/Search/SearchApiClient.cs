// Forum.Web/Features/Search/SearchApiClient.cs
using System.Net;
using System.Net.Http.Json;

namespace Forum.Web.Features.Search;

public interface ISearchApiClient
{
    Task<SearchOutcome<PagedSearchResponse>> SearchAsync(
        string q, int? categoryId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
}

/// <summary>
/// Typed client for the API's search endpoint. Injected via IHttpClientFactory
/// (configured in SearchModule). The endpoint is anonymous, so no token is needed,
/// but the shared AuthTokenHandler rides along for template consistency.
/// </summary>
public sealed class SearchApiClient(HttpClient http) : ISearchApiClient
{
    public async Task<SearchOutcome<PagedSearchResponse>> SearchAsync(
        string q, int? categoryId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/search?q={Uri.EscapeDataString(q)}&page={page}&pageSize={pageSize}";
            if (categoryId is not null)
                url += $"&categoryId={categoryId}";

            using var response = await http.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<PagedSearchResponse>(cancellationToken: ct);
                return data is not null
                    ? SearchOutcome<PagedSearchResponse>.Ok(data)
                    : SearchOutcome<PagedSearchResponse>.Failed("The server returned an empty response.");
            }

            return SearchOutcome<PagedSearchResponse>.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return SearchOutcome<PagedSearchResponse>.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return SearchOutcome<PagedSearchResponse>.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
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

        return response.StatusCode == HttpStatusCode.BadRequest
            ? "Search term must be at least 2 characters."
            : $"Request failed ({(int)response.StatusCode}).";
    }

    private record ApiError(string? Error);
}
