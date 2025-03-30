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