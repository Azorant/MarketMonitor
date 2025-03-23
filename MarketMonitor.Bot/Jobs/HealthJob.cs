using Discord;
using MarketMonitor.Bot.Services;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class HealthJob(UniversalisWebsocket ws, StatusService status)
{
    public async Task CheckHealth()
    {
        if (ws.IsAlive) return;

        Log.Warning("Universalis websocket died, starting back up");
        await status.SendUpdate("Universalis WS", "Connection died, starting back up", Color.Red);
        ws.Connect();
    }
}