// Forum.Web/Features/Auth/AuthApiClient.cs
using System.Net;
using System.Net.Http.Json;

namespace Forum.Web.Features.Auth;

public interface IAuthApiClient
{
    Task<AuthOutcome> RegisterAsync(RegisterModel model, CancellationToken ct = default);
    Task<AuthOutcome> LoginAsync(LoginModel model, CancellationToken ct = default);
}

/// <summary>
/// Typed client for the API's auth endpoints. Injected via IHttpClientFactory
/// (configured in Program.cs). Translates HTTP responses into AuthOutcome so
/// components never see raw exceptions for expected failures (409/401/400).
/// </summary>
public sealed class AuthApiClient(HttpClient http) : IAuthApiClient
{
    public Task<AuthOutcome> RegisterAsync(RegisterModel model, CancellationToken ct = default) =>
        PostAsync("api/auth/register",
            new RegisterRequest(model.UserName, model.Email, model.Password), ct);

    public Task<AuthOutcome> LoginAsync(LoginModel model, CancellationToken ct = default) =>
        PostAsync("api/auth/login",
            new LoginRequest(model.UserNameOrEmail, model.Password), ct);

    private async Task<AuthOutcome> PostAsync<TRequest>(string uri, TRequest body, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(uri, body, ct);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
                return data is not null
                    ? AuthOutcome.Ok(data)
                    : AuthOutcome.Failed("The server returned an empty response.");
            }

            return AuthOutcome.Failed(await ReadErrorAsync(response, ct));
        }
        catch (HttpRequestException)
        {
            return AuthOutcome.Failed("Could not reach the server. Please try again.");
        }
        catch (TaskCanceledException)
        {
            return AuthOutcome.Failed("The request timed out. Please try again.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        // 400 from the FluentValidation filter is a ValidationProblemDetails.
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken: ct);
            var firstError = problem?.Errors?.Values.SelectMany(v => v).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstError))
                return firstError;
        }

        // 409 / 401 return { "error": "..." }.
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
