using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WebSocketSharp;

namespace MarketMonitor.Bot.Services;

public class UniversalisWebsocket
{
    private readonly WebSocket client;
    private readonly IServiceProvider serviceProvider;
    private readonly CacheService cache;

    public UniversalisWebsocket(IServiceProvider serviceProvider, CacheService cache)
    {
        this.serviceProvider = serviceProvider;
        this.cache = cache;
        client = new WebSocket("wss://universalis.app/api/ws");

        client.OnMessage += OnPacket;
        client.OnOpen += OnOpen;
    }

    private async void OnPacket(object? sender, MessageEventArgs args)
    {
        try
        {
            // Task.Run(async () =>
            // {
            var packet = Serializer.Deserialize<DataPacket>(args.RawData);
            if (packet == null) return;
            var db = serviceProvider.GetRequiredService<DatabaseContext>();

            var changes = false;

            switch (packet.Event)
            {
                case "listings/add":
                {
                    // var trackedRetainers = await db.Retainers.Where(r => packet.Listings!.Select(l => l.RetainerId).Contains(r.Id)).AsNoTracking().ToListAsync();
                    foreach (var listing in packet.Listings!)
                    {
                        var tracking = await cache.GetRetainer(listing.RetainerName);
                        if (!tracking) continue;
                        var existing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listing.ListingId);
                        if (existing == null)
                        {
                            await db.AddAsync(new ListingEntity
                            {
                                Id = listing.ListingId,
                                ItemId = packet.Item,
                                PricePerUnit = listing.PricePerUnit,
                                Quantity = listing.Quantity,
                                UpdatedAt = listing.LastReviewTime.ConvertTimestamp(),
                                RetainerId = listing.RetainerId
                            });
                            changes = true;
                        }
                        else if (existing.IsRemoved)
                        {
                            existing.IsRemoved = false;
                            db.Update(existing);
                            changes = true;
                        }
                    }

                    break;
                }
                case "listings/remove":
                {
                    // var trackedRetainers = await db.Retainers.Where(r => packet.Listings!.Select(l => l.RetainerId).Contains(r.Id)).AsNoTracking().ToListAsync();
                    foreach (var listing in packet.Listings!)
                    {
                        var tracking = await cache.GetRetainer(listing.RetainerName);
                        if (!tracking) continue;
                        var existing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listing.ListingId);
                        if (existing == null || existing.IsRemoved) continue;

                        existing.IsRemoved = true;
                        db.Update(existing);
                        changes = true;
                    }

                    break;
                }
                case "sales/add":
                {
                    // var trackedCharacters = await db.Characters.Where(c => packet.Sales!.Select(s => s.BuyerName).Distinct().Contains(c.Name)).AsNoTracking().ToListAsync();

                    foreach (var sale in packet.Sales!)
                    {
                        var (tracking, id) = await cache.GetCharacter(sale.BuyerName);
                        if (!tracking) continue;
                        await db.AddAsync(new PurchaseEntity
                        {
                            ItemId = packet.Item,
                            WorldId = packet.World,
                            Quantity = sale.Quantity,
                            PricePerUnit = sale.PricePerUnit,
                            IsHq = sale.Hq,
                            PurchasedAt = sale.Timestamp.ConvertTimestamp(),
                            CharacterId = id
                        });
                        changes = true;
                    }

                    break;
                }
            }

            if (changes) await db.SaveChangesAsync();
            // await db.DisposeAsync();
            // }).ContinueWith(t =>
            // {
            //     if (t.Exception != null) Log.Error(t.Exception, "Error while consuming universalis websocket");
            // });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while consuming universalis websocket");
        }
    }

    private async void OnOpen(object? sender, EventArgs eventArgs)
    {
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "listings/add")));
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "listings/remove")));
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "sales/add")));
    }

    public void Connect() => client.Connect();
}