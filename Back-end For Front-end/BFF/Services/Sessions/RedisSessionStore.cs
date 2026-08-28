using System.Text.Json;
using BFF.Models.Sessions;
using StackExchange.Redis;

namespace BFF.Services.Sessions;

public sealed class RedisSessionStore(IConnectionMultiplexer connectionMultiplexer) : ISessionStore
{
    private const string KeyPrefix = "yalla:bff:session:";
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task StoreAsync(UserSession session, TimeSpan lifetime, CancellationToken cancellationToken = default) =>
        await _database.StringSetAsync(GetKey(session.SessionId), JsonSerializer.Serialize(session), lifetime);

    public async Task<UserSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(GetKey(sessionId));
        return value.HasValue ? JsonSerializer.Deserialize<UserSession>(value.ToString()) : null;
    }

    public async Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default) =>
        await _database.KeyDeleteAsync(GetKey(sessionId));

    private static RedisKey GetKey(string sessionId) => $"{KeyPrefix}{sessionId}";
}
