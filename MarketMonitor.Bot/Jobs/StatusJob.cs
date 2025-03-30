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

public class StatusJob(DiscordSocketClient client, PrometheusService stats, DatabaseContext db)
{
    private int lastStatus;

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
}