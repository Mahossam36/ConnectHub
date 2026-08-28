using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BFF.Configuration;
using Microsoft.Extensions.Options;

namespace BFF.Services.Moderation;

public sealed class OpenAiContentModerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiContentModerationService> logger) : IContentModerationService
{
    private readonly OpenAiOptions _options = options.Value;

    public async Task<ContentModerationResult> ModerateAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogWarning("Content moderation is unavailable because its server-side configuration is incomplete.");
            return ContentModerationResult.Unavailable;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "moderations")
            {
                Content = JsonContent.Create(new { model = _options.Moderation.Model, input = content })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            using var response = await httpClientFactory.CreateClient("OpenAI").SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Content moderation service returned status {StatusCode}.", (int)response.StatusCode);
                return ContentModerationResult.Unavailable;
            }

            using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!payload.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array ||
                results.GetArrayLength() == 0 || !results[0].TryGetProperty("flagged", out var flag) ||
                flag.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                logger.LogWarning("Content moderation service returned an unusable response.");
                return ContentModerationResult.Unavailable;
            }

            var flagged = flag.GetBoolean();
            logger.LogInformation("Content moderation completed with outcome {Outcome}.", flagged ? "flagged" : "allowed");
            return flagged ? ContentModerationResult.Flagged : ContentModerationResult.Allowed;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Content moderation timed out.");
            return ContentModerationResult.Unavailable;
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Content moderation service is unavailable.");
            return ContentModerationResult.Unavailable;
        }
        catch (JsonException)
        {
            logger.LogWarning("Content moderation service returned an invalid response.");
            return ContentModerationResult.Unavailable;
        }
    }
}
