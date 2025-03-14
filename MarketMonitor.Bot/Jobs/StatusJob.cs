using Discord.WebSocket;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class StatusJob(DiscordSocketClient client, PrometheusService stats, DatabaseContext db)
{
    private int lastStatus;
    private readonly string[] statuses = ["/help", "eris.gg"];

    public async Task SetStatus()
    {
        try
        {
            await client.SetCustomStatusAsync(statuses[lastStatus]);
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