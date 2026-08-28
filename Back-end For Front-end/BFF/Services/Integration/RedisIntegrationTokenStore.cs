using StackExchange.Redis;

namespace BFF.Services.Integration;

/// <summary>Shared client-level WSO2 token cache; it is deliberately independent of user sessions.</summary>
public sealed class RedisIntegrationTokenStore(IConnectionMultiplexer connectionMultiplexer) : IIntegrationTokenStore
{
    private const string Key = "yalla:bff:integration-token";
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<string?> GetAsync(CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(Key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetAsync(string token, TimeSpan lifetime, CancellationToken cancellationToken = default) =>
        await _database.StringSetAsync(Key, token, lifetime);

    public async Task RemoveAsync(CancellationToken cancellationToken = default) =>
        await _database.KeyDeleteAsync(Key);
}
