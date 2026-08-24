using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ConnectHub.ApiTestRunner;

public sealed class ApiClient
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ApiClient(RunnerOptions options)
    {
        var handler = new HttpClientHandler();
        if (options.AllowUntrustedDevelopmentCertificate)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        _client = new HttpClient(handler) { BaseAddress = options.BaseUrl, Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<ApiResponse> SendAsync(HttpMethod method, string path, object? body = null, string? token = null, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content ?? (body is null ? null : new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json"));

        var timer = Stopwatch.StartNew();
        var response = await _client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        timer.Stop();
        return new ApiResponse(response.StatusCode, responseBody, response, timer.ElapsedMilliseconds);
    }

    public static JsonElement Json(string body) => JsonDocument.Parse(body).RootElement.Clone();

    public static Guid RequiredGuid(JsonElement root, string property) => root.GetProperty(property).GetGuid();
    public static string RequiredString(JsonElement root, string property) => root.GetProperty(property).GetString() ?? throw new InvalidOperationException($"Missing '{property}'.");
}
