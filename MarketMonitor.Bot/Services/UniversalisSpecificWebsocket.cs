using Hangfire;
using MarketMonitor.Bot.Jobs;
using MarketMonitor.Bot.Models.Universalis;
using WebSocketSharp;

namespace MarketMonitor.Bot.Services;

public class UniversalisSpecificWebsocket(HandleSaleJob saleJob) : UniversalisWebsocket
{
    public override async void OnPacket(object? sender, MessageEventArgs args)
    {
        var packet = Serializer.Deserialize<DataPacket>(args.RawData);
        if (packet?.Event != "sales/add") return;
        Console.WriteLine($"Received packet: {packet}");
        foreach (var sale in packet.Sales!)
        {
            BackgroundJob.Schedule(() => saleJob.Handle(packet.Item, packet.World, sale), TimeSpan.FromMinutes(1));
        }
    }

    public override void OnOpen(object? sender, EventArgs args)
    {
        base.OnOpen(sender, args);
        RecurringJob.TriggerJob("alerts");
    }
}