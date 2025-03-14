﻿using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

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
            var db = serviceProvider.GetRequiredService<DatabaseContext>();

            var changes = false;

            switch (packet.Event)
            {
                case "listings/add":
                {
                    var retainers = new List<RetainerEntity>();
                    foreach (var listing in packet.Listings!)
                    {
                        var tracking = await cache.GetRetainer(listing.RetainerName);
                        if (!tracking) continue;
                        var existing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listing.ListingId);
                        if (existing == null)
                        {
                            var retainer = retainers.FirstOrDefault(r => r.Id == listing.RetainerId);
                            if (retainer == null)
                            {
                                retainer = await db.Retainers.AsNoTracking().FirstOrDefaultAsync(r => r.Id == listing.RetainerId);
                                if (retainer == null) continue;
                                retainers.Add(retainer);
                            }

                            await db.AddAsync(new ListingEntity
                            {
                                Id = listing.ListingId,
                                ItemId = packet.Item,
                                PricePerUnit = listing.PricePerUnit,
                                Quantity = listing.Quantity,
                                UpdatedAt = listing.LastReviewTime.ConvertTimestamp(),
                                RetainerName = listing.RetainerName,
                                RetainerOwnerId = retainer.OwnerId,
                                WorldId = packet.World,
                                IsHq = listing.Hq
                            });
                            changes = true;
                        }
                        else
                        {
                            var updated = false;
                            var relisted = false;
                            if (existing.IsRemoved)
                            {
                                existing.IsRemoved = false;
                                relisted = true;
                            }

                            if (existing.Quantity != listing.Quantity)
                            {
                                existing.Quantity = listing.Quantity;
                                updated = true;
                            }

                            if (existing.PricePerUnit != listing.PricePerUnit)
                            {
                                existing.PricePerUnit = listing.PricePerUnit;
                                updated = true;
                            }

                            if (!updated && !relisted)
                                continue;
                            if (updated) existing.IsNotified = false;
                            db.Update(existing);
                            changes = true;
                        }
                    }

                    break;
                }
                case "listings/remove":
                {
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
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while consuming universalis websocket");
        }
    }

    private void OnOpen(object? sender, EventArgs eventArgs)
    {
        Log.Information("Universalis websocket connected");
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "listings/add")));
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "listings/remove")));
        client.Send(Serializer.Serialize(new SubscribePacket("subscribe", "sales/add")));
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        Log.Error(e.Exception, "Universalis websocket error");
    }

    private async void OnClose(object? sender, CloseEventArgs e)
    {
        Log.Warning($"Universalis websocket connection closed {e.Code} - {e.Reason}");
        await Task.Delay(5000);
        Connect();
    }

    public void Connect() => client.Connect();
}