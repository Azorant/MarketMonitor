using Discord;
using Discord.WebSocket;
using Hangfire;
using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public struct Status(string text, ActivityType type = ActivityType.CustomStatus)
{
    public ActivityType Type { get; set; } = type;
    public string Text { get; set; } = text;
}

public class StatusJob(DiscordSocketClient client, PrometheusService stats, DatabaseContext db, UniversalisSpecificWebsocket websocket)
{
    private int lastStatus;
    private List<(int, int)> watchedItems = new();

    private readonly Status[] statuses =
    [
        new("/character setup"), new("{characters}", ActivityType.Watching), new("/retainer setup"), new("{retainers}", ActivityType.Watching), new("eris.gg")
    ];

    [TypeFilter(typeof(LogExecutionAttribute)), DisableConcurrentExecution("status", 10)]
    public async Task SetStatus()
    {
        try
        {
            var status = statuses[lastStatus];
            if (status.Text.Contains("{characters}"))
            {
                var amount = await db.Characters.Where(c => c.IsVerified).CountAsync();
                await client.SetGameAsync($"{amount:N0} {"character".Quantize(amount)}", type: status.Type);
            }
            else if (status.Text.Contains("{retainers}"))
            {
                var amount = await db.Retainers.Where(c => c.IsVerified).CountAsync();
                await client.SetGameAsync($"{amount:N0} {"retainer".Quantize(amount)}", type: status.Type);
            }
            else
            {
                await client.SetGameAsync(status.Text, type: status.Type);
            }

            lastStatus++;
            if (lastStatus == statuses.Length) lastStatus = 0;
            stats.Latency.Set(client.Latency);
            stats.TrackedListings.Set(await db.Listings.CountAsync());
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to set status");
        }
    }

    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task SetupAlerts()
    {
        var listings = await db.Listings
            .Where(l => l.Flags == ListingFlags.None)
            .GroupBy(l => l.ItemId)
            .Select(l => new { ItemId = l.Key, WorldIds = l.Select(x => x.WorldId) })
            .ToListAsync();

        var newItems = new List<(int, int)>();

        foreach (var listing in listings)
        {
            var worldIds = listing.WorldIds.Distinct();
            foreach (var worldId in worldIds)
            {
                var pair = (worldId, listing.ItemId);
                if (watchedItems.Contains(pair))
                {
                    newItems.Add(pair);
                    continue;
                }

                newItems.Add(pair);
                websocket.SendPacket(new SubscribePacket("subscribe", $"sales/add{{world={worldId}, item={listing.ItemId}}}"));
                Log.Information($"Subscribed to alerts in {worldId} for {listing.ItemId}");
            }
        }

        var missing = watchedItems.Except(newItems).ToList();

        foreach (var pair in missing)
        {
            websocket.SendPacket(new SubscribePacket("unsubscribe", $"sales/add{{world={pair.Item1}, item={pair.Item2}}}"));
            Log.Information($"Unsubscribed from alerts in {pair.Item1} for {pair.Item2}");

        }

        watchedItems = newItems;
    }
}