using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class StatusJob(IServiceProvider serviceProvider)
{
    private int lastStatus;
    private readonly string[] statuses = ["/help", "eris.gg"];

    public async Task SetStatus()
    {
        try
        {
            var client = serviceProvider.GetRequiredService<DiscordSocketClient>();
            await client.SetCustomStatusAsync(statuses[lastStatus]);
            lastStatus++;
            if (lastStatus == statuses.Length) lastStatus = 0;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to set status");
        }
    }
}