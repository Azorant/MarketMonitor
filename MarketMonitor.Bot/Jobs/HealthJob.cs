using Discord;
using Hangfire;
using MarketMonitor.Bot.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class HealthJob(UniversalisGeneralWebsocket ws, StatusService status)
{
    [TypeFilter(typeof(LogExecutionAttribute)), DisableConcurrentExecution("health", 10)]
    public async Task CheckHealth()
    {
        if (ws.IsAlive || ws.FirstConnect) return;

        Log.Warning("Universalis websocket died, starting back up");
        await status.SendUpdate("Universalis WS", "Connection died, starting back up", Color.Red);
        ws.Connect();
    }
}