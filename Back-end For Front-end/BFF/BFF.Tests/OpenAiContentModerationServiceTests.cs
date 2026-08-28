using System.Net;
using System.Text;
using BFF.Configuration;
using BFF.Services.Moderation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BFF.Tests;

public sealed class OpenAiContentModerationServiceTests
{
    [Fact]
    public async Task ModerateAsync_AllowsSafeContentAndSendsOnlySubmittedText()
    {
        string? submittedBody = null;
        var service = CreateService(async request =>
        {
            submittedBody = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, "{\"results\":[{\"flagged\":false}]}");
        });

        var result = await service.ModerateAsync("safe text");

        Assert.True(result.IsAllowed);
        Assert.False(result.IsUnavailable);
        Assert.Equal("{\"model\":\"omni-moderation-latest\",\"input\":\"safe text\"}", submittedBody);
    }

    [Fact]
    public async Task ModerateAsync_RejectsFlaggedContent()
    {
        var service = CreateService(_ => Task.FromResult(Json(HttpStatusCode.OK, "{\"results\":[{\"flagged\":true}]}")));

        var result = await service.ModerateAsync("unsafe text");

        Assert.False(result.IsAllowed);
        Assert.False(result.IsUnavailable);
    }

    [Fact]
    public async Task ModerateAsync_FailsClosedWhenServiceIsUnavailableOrMalformed()
    {
        var unavailable = CreateService(_ => Task.FromResult(Json(HttpStatusCode.TooManyRequests, "{}")));
        var malformed = CreateService(_ => Task.FromResult(Json(HttpStatusCode.OK, "{}")));

        var unavailableResult = await unavailable.ModerateAsync("content");
        var malformedResult = await malformed.ModerateAsync("content");

        Assert.True(unavailableResult.IsUnavailable);
        Assert.True(malformedResult.IsUnavailable);
    }

    private static OpenAiContentModerationService CreateService(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) => new(
        new TestHttpClientFactory(new TestHandler(send)),
        Options.Create(new OpenAiOptions { ApiKey = "test-key", Moderation = new ModerationOptions { Model = "omni-moderation-latest", TimeoutSeconds = 10 } }),
        NullLogger<OpenAiContentModerationService>.Instance);

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) => new(statusCode)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.openai.com/v1/") };
    }

    private sealed class TestHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
