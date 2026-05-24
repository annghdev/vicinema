using System.Text.Json;
using CinemaTicketBooking.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace CinemaTicketBooking.Infrastructure.Cache;

public class RedisCacheService(
    IDistributedCache distributedCache,
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<RedisCacheOptions> redisOptions) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly string _instanceName = redisOptions.Value.InstanceName ?? string.Empty;

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var json = await distributedCache.GetStringAsync(key, ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions();
        if (slidingExpiration.HasValue)
        {
            options.SlidingExpiration = slidingExpiration;
        }

        var json = JsonSerializer.Serialize(value, SerializerOptions);
        await distributedCache.SetStringAsync(key, json, options, ct);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        return distributedCache.RemoveAsync(key, ct);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken ct = default)
    {
        return RemoveKeysByPatternAsync($"{_instanceName}*", ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var db = connectionMultiplexer.GetDatabase();
        return await db.KeyExistsAsync($"{_instanceName}{key}");
    }

    /// <inheritdoc />
    public Task RemoveByPrefix(string prefix, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return Task.CompletedTask;
        }

        return RemoveKeysByPatternAsync($"{_instanceName}{prefix}*", ct);
    }

    private async Task RemoveKeysByPatternAsync(string pattern, CancellationToken ct)
    {
        var db = connectionMultiplexer.GetDatabase();
        var keys = new List<RedisKey>();

        foreach (var endpoint in connectionMultiplexer.GetEndPoints())
        {
            ct.ThrowIfCancellationRequested();
            var server = connectionMultiplexer.GetServer(endpoint);

            if (!server.IsConnected || server.IsReplica)
            {
                continue;
            }

            await foreach (var key in server.KeysAsync(db.Database, pattern: pattern))
            {
                ct.ThrowIfCancellationRequested();
                keys.Add(key);
            }
        }

        if (keys.Count == 0)
        {
            return;
        }

        await db.KeyDeleteAsync(keys.ToArray());
    }
}
