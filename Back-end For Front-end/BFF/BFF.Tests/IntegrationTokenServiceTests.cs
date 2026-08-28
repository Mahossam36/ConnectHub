using System.Text;
using System.Text.Json;
using BFF.Configuration;
using BFF.Models.Sessions;
using BFF.Services.Integration;
using Microsoft.Extensions.Options;
using Xunit;

namespace BFF.Tests;

public sealed class IntegrationTokenServiceTests
{
    [Fact]
    public async Task GetValidAsync_ReusesCachedUnexpiredToken()
    {
        var client = new FakeClient();
        var token = Jwt(DateTimeOffset.UtcNow.AddMinutes(10));
        var service = CreateService(client, new FakeStore(token));

        var result = await service.GetValidAsync();

        Assert.Equal(token, result);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task GetValidAsync_ReplacesExpiredToken()
    {
        var client = new FakeClient(Jwt(DateTimeOffset.UtcNow.AddMinutes(10)));
        var store = new FakeStore(Jwt(DateTimeOffset.UtcNow.AddMinutes(-1)));
        var service = CreateService(client, store);

        var result = await service.GetValidAsync();

        Assert.Equal(1, client.Calls);
        Assert.Equal(result, store.Token);
    }

    [Fact]
    public async Task GetValidAsync_ConcurrentCallsAcquireOnlyOnce()
    {
        var client = new FakeClient(Jwt(DateTimeOffset.UtcNow.AddMinutes(10)), TimeSpan.FromMilliseconds(25));
        var service = CreateService(client, new FakeStore());

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => service.GetValidAsync()));

        Assert.Equal(1, client.Calls);
    }

    private static IntegrationTokenService CreateService(FakeClient client, FakeStore store) => new(
        client, store, Options.Create(new IntegrationOptions { IntegrationTokenExpirationSafetyMarginSeconds = 30 }));

    private static string Jwt(DateTimeOffset expiresAt)
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { exp = expiresAt.ToUnixTimeSeconds() })))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"header.{payload}.signature";
    }

    private sealed class FakeStore(string? token = null) : IIntegrationTokenStore
    {
        public string? Token { get; private set; } = token;
        public Task<string?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Token);
        public Task SetAsync(string token, TimeSpan lifetime, CancellationToken cancellationToken = default) { Token = token; return Task.CompletedTask; }
        public Task RemoveAsync(CancellationToken cancellationToken = default) { Token = null; return Task.CompletedTask; }
    }

    private sealed class FakeClient(string? token = null, TimeSpan? delay = null) : IIntegrationTokenClient
    {
        private int _calls;
        public int Calls => _calls;
        public async Task<IntegrationTokenResult> AcquireAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            if (delay is not null) await Task.Delay(delay.Value, cancellationToken);
            return new IntegrationTokenResult(token ?? Jwt(DateTimeOffset.UtcNow.AddMinutes(10)), null);
        }

        public Task RevokeAsync(string token, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
