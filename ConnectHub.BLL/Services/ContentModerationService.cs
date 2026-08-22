using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.Result;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class ContentModerationService : IContentModerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContentModerationService> _logger;

    public ContentModerationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ContentModerationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<bool>> IsContentSafeAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Success(true);

        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogDebug("OpenAI:ApiKey is not configured; skipping external content moderation.");
            return Result.Success(true);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/moderations");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = JsonSerializer.Serialize(new { input = content });
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OpenAI Moderation API returned non-success status code: {StatusCode}. Response: {Response}", response.StatusCode, errorBody);
                // Fail-safe: don't block user if moderation service is temporarily unavailable
                return Result.Success(true);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OpenAiModerationResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var isFlagged = result?.Results?.FirstOrDefault()?.Flagged ?? false;
            if (isFlagged)
            {
                _logger.LogWarning("User-generated content flagged by moderation policy.");
                return Result.Invalid(new ValidationError("The submitted content violates community safety guidelines."));
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during content safety moderation.");
            // On external API failure, allow operation to continue but log the issue
            return Result.Success(true);
        }
    }

    private class OpenAiModerationResponse
    {
        [JsonPropertyName("results")]
        public List<ModerationResult>? Results { get; set; }
    }

    private class ModerationResult
    {
        [JsonPropertyName("flagged")]
        public bool Flagged { get; set; }
    }
}
