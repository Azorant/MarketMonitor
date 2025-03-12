using MarketMonitor.Database;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace MarketMonitor.Bot.Services;

public class CacheService(ConnectionMultiplexer redis)
{
    private string BuildKey(string key) => $"{Environment.GetEnvironmentVariable("PREFIX") ?? "marketmonitor"}:{key}";

    public async Task<(bool, ulong)> GetCharacter(string name)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(BuildKey($"character:{name}"));
        return (!value.IsNullOrEmpty, value.IsNullOrEmpty ? 0 : ulong.Parse(value!));
    }

    public async Task<bool> GetRetainer(string name)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(BuildKey($"retainer:{name}"));
        return !value.IsNullOrEmpty;
    }

    public async Task SetCharacter(string name, ulong id)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(BuildKey($"character:{name}"), id, expiry: TimeSpan.FromMinutes(30));
    }

    public async Task SetRetainer(string name)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(BuildKey($"retainer:{name}"), true, expiry: TimeSpan.FromMinutes(30));
    }
}