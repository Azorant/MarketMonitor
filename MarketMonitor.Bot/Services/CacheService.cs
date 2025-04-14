using System.Text.Json;
using Discord;
using Discord.WebSocket;
using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Database;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using StackExchange.Redis;

namespace MarketMonitor.Bot.Services;

public class CacheService(ConnectionMultiplexer redis, DiscordSocketClient client)
{
    public Dictionary<string, Emote> Emotes { get; set; } = new();

    private string BuildKey(params object[] args)
    {
        return BuildKey(string.Join(':', args.Select(a => a.ToString())));
    }

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

    public async Task<bool> GetListing(int itemId, int worldId)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(BuildKey($"listing:{itemId}:{worldId}"));
        return !value.IsNullOrEmpty;
    }

    public async Task SetListing(int itemId, int worldId)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(BuildKey($"listing:{itemId}:{worldId}"), true, expiry: TimeSpan.FromSeconds(40));
    }

    public async Task<string?> GetAvatar(string name)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(BuildKey($"avatar:{name}"));
        return value.IsNullOrEmpty ? null : (string)value!;
    }

    public async Task SetAvatar(string name, string url)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(BuildKey($"avatar:{name}"), url);
    }

    public async Task<bool> MarketSaleCache(int itemId, string datacenter, SaleData saleData)
    {
        var db = redis.GetDatabase();
        var key = BuildKey("market", itemId, datacenter, saleData.Timestamp, saleData.Quantity, saleData.PricePerUnit, saleData.Hq, saleData.BuyerName);

        var value = await db.StringGetAsync(key);
        if (!value.IsNullOrEmpty)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromHours(12));
            return true;
        }

        await db.StringSetAsync(key, true, TimeSpan.FromHours(12));
        return false;
    }

    public async Task<CityTaxRates?> GetTaxRate(int worldId)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(BuildKey("tax", worldId));
        return !value.IsNullOrEmpty ? JsonSerializer.Deserialize<CityTaxRates>(value!) : null;
    }

    public async Task SetTaxRate(int worldId, CityTaxRates obj)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(BuildKey("tax", worldId), JsonSerializer.Serialize(obj), expiry: obj.ResetsIn());
    }

    public async Task LoadApplicationEmotes()
    {
        var emotes = await client.GetApplicationEmotesAsync();
        foreach (var emote in emotes)
        {
            Emotes.Add(emote.Name, emote);
        }

        Log.Information($"Loaded {Emotes.Count} application emotes");
    }
}