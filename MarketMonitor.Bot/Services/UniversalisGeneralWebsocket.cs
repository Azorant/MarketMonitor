using Discord;
using Hangfire;
using MarketMonitor.Bot.Jobs;
using MarketMonitor.Bot.Models.Universalis;
using Serilog;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace MarketMonitor.Bot.Services;

public class UniversalisGeneralWebsocket
{
    private readonly WebSocket client;
    private readonly CacheService cache;
    private readonly PacketJob job;
    private readonly StatusService statusService;
    public bool FirstConnect { get; set; } = true;

    public UniversalisGeneralWebsocket(CacheService cache, PacketJob job, StatusService statusService)
    {
        this.cache = cache;
        this.job = job;
        this.statusService = statusService;
        client = new WebSocket("wss://universalis.app/api/ws");
        client.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

        client.OnMessage += OnPacket;
        client.OnOpen += OnOpen;
        client.OnClose += OnClose;
        client.OnError += OnError;
    }

    private async void OnPacket(object? sender, MessageEventArgs args)
    {
        try
        {
            var packet = Serializer.Deserialize<DataPacket>(args.RawData);
            if (packet == null) return;

            switch (packet.Event)
            {
                case "listings/add":
                {
                    var groups = packet.Listings!.GroupBy(l => l.RetainerId);

                    foreach (var retainerGroup in groups)
                    {
                        var tracking = await cache.GetRetainer(retainerGroup.First().RetainerName);
                        if (!tracking) continue;
                        BackgroundJob.Enqueue(() => job.HandleListingAdd(retainerGroup.Key, packet.Item, packet.World, retainerGroup.ToList()));
                    }

                    break;
                }
                case "listings/remove":
                {
                    var groups = packet.Listings!.GroupBy(l => l.RetainerId);

                    foreach (var retainerGroup in groups)
                    {
                        var tracking = await cache.GetRetainer(retainerGroup.First().RetainerName);
                        if (!tracking) continue;
                        BackgroundJob.Enqueue(() => job.HandleListingRemove(retainerGroup.Key, packet.World, retainerGroup.Select(l => new RemovedListing(l.ListingId, l.LastReviewTime)).ToList()));
                    }

                    break;
                }
                case "sales/add":
                {
                    var groups = packet.Sales!.GroupBy(s => s.BuyerName);

                    foreach (var buyerGroup in groups)
                    {
                        var (tracking, id) = await cache.GetCharacter(buyerGroup.Key);
                        if (!tracking) continue;
                        BackgroundJob.Enqueue(() => job.HandlePurchaseAdd(id, packet.Item, packet.World, buyerGroup.ToList()));
                    }

                    var trackedListing = await cache.GetListing(packet.Item, packet.World);
                    if (!trackedListing) break;

                    foreach (var sale in packet.Sales!)
                    {
                        BackgroundJob.Schedule(() => job.HandleSaleAdd(packet.Item, packet.World, sale), TimeSpan.FromMinutes(5));
                    }
                    
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while consuming universalis websocket");
        }
    }

    private void OnOpen(object? sender, EventArgs eventArgs)
    {
        Log.Information("Universalis websocket connected");
        _ = statusService.SendUpdate("Universalis WS", "Connected", Color.Green);
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "listings/add")));
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "listings/remove")));
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "sales/add")));
        FirstConnect = false;
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        Log.Error(e.Exception, "Universalis websocket error");
    }

    private async void OnClose(object? sender, CloseEventArgs e)
    {
        Log.Warning($"Universalis websocket connection closed {e.Code} - {e.Reason}");
        await statusService.SendUpdate("Universalis WS", "Connection closed", Color.Gold);
        await Task.Delay(5000);
        Connect();
    }

    public void Connect() => client.Connect();
    public bool IsAlive => client.IsAlive;
}