using Discord;
using Discord.WebSocket;
using Hangfire;
using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using MarketMonitor.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public struct RemovedListing(string id, double timestamp, int quantity, int ppu)
{
    public string Id { get; set; } = id;
    public double Timestamp { get; set; } = timestamp;
    public int Quantity { get; set; } = quantity;
    public int PricePerUnit { get; set; } = ppu;
}

public class PacketJob(IServiceProvider serviceProvider)
{
    [DisableConcurrentExecution("listing", 60), TypeFilter(typeof(LogExecutionAttribute))]
    public async Task HandleListingAdd(string retainerId, int itemId, int worldId, List<ListingData> listings)
    {
        await using var db = serviceProvider.GetRequiredService<DatabaseContext>();
        var retainer = await db.Retainers.AsNoTracking().FirstOrDefaultAsync(r => r.Id == retainerId || (r.Name == listings.First().RetainerName && string.IsNullOrEmpty(r.Id)));
        if (retainer == null) return;
        if (string.IsNullOrEmpty(retainer.Id))
        {
            retainer.Id = retainerId;
            db.Update(retainer);
        }

        var existingListings = await db.Listings.Where(l => listings.Select(e => e.ListingId).Contains(l.Id)).ToListAsync();

        var apiService = serviceProvider.GetRequiredService<ApiService>();
        var taxRates = await apiService.FetchTaxRate(worldId);
        foreach (var listing in listings)
        {
            var existing = existingListings.FirstOrDefault(l => l.Id == listing.ListingId);
            if (existing == null)
            {
                await db.AddAsync(new ListingEntity
                {
                    Id = listing.ListingId,
                    ItemId = itemId,
                    PricePerUnit = listing.PricePerUnit,
                    Quantity = listing.Quantity,
                    UpdatedAt = listing.LastReviewTime.ConvertTimestamp(),
                    RetainerName = listing.RetainerName,
                    RetainerOwnerId = retainer.OwnerId,
                    WorldId = worldId,
                    IsHq = listing.Hq,
                    TaxRate = 1 - taxRates.GetCityRate(listing.RetainerCity) / 100d,
                    RetainerCity = listing.RetainerCity,
                });
            }
            else
            {
                var updated = false;
                var relisted = false;
                if (existing.Flags.HasFlag(ListingFlags.Removed))
                {
                    existing.Flags = existing.Flags.RemoveFlag(ListingFlags.Removed);
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
                if (updated)
                    existing.IsNotified = false;

                existing.TaxRate = 1 - taxRates.GetCityRate(listing.RetainerCity) / 100d;
                existing.UpdatedAt = listing.LastReviewTime.ConvertTimestamp();

                db.Update(existing);
            }
        }

        await db.SaveChangesAsync();
    }

    [DisableConcurrentExecution("listing", 60), TypeFilter(typeof(LogExecutionAttribute))]
    public async Task HandleListingRemove(string retainerId, int worldId, List<RemovedListing> removedListings)
    {
        await using var db = serviceProvider.GetRequiredService<DatabaseContext>();
        var retainer = await db.Retainers.AsNoTracking().FirstOrDefaultAsync(r => r.Id == retainerId);
        if (retainer == null) return;
        var ids = removedListings.Select(l => l.Id).ToList();
        var listings = await db.Listings.Where(l => ids.Contains(l.Id) && !l.Flags.HasFlag(ListingFlags.Removed)).ToListAsync();

        var apiService = serviceProvider.GetRequiredService<ApiService>();
        var taxRates = await apiService.FetchTaxRate(worldId);
        foreach (var listing in listings)
        {
            var found = removedListings.Find( l => l.Id == listing.Id);
            listing.Quantity = found.Quantity;
            listing.PricePerUnit = found.PricePerUnit;
            listing.Flags = listing.Flags.AddFlag(ListingFlags.Removed);
            listing.UpdatedAt = found.Timestamp.ConvertTimestamp();
            listing.TaxRate = 1 - taxRates.GetCityRate(listing.RetainerCity) / 100d;
            db.Update(listing);
        }

        await db.SaveChangesAsync();
    }

    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task HandlePurchaseAdd(ulong buyerId, int itemId, int worldId, List<SaleData> sales)
    {
        await using var db = serviceProvider.GetRequiredService<DatabaseContext>();
        await db.AddRangeAsync(sales.Select(sale => new PurchaseEntity
        {
            ItemId = itemId,
            WorldId = worldId,
            Quantity = sale.Quantity,
            PricePerUnit = sale.PricePerUnit,
            IsHq = sale.Hq,
            PurchasedAt = sale.Timestamp.ConvertTimestamp(),
            CharacterId = buyerId
        }));
        await db.SaveChangesAsync();
    }

    [DisableConcurrentExecution("sales", 60), TypeFilter(typeof(LogExecutionAttribute))]
    public async Task HandleSaleAdd(int itemId, int worldId, SaleData sale)
    {
        try
        {
            await using var db = serviceProvider.GetRequiredService<DatabaseContext>();
            var listings = await db.Listings
                .Where(l => l.ItemId == itemId && l.WorldId == worldId && l.Flags == ListingFlags.Removed)
                .OrderByDescending(l => l.UpdatedAt)
                .ToListAsync();

            ListingEntity? listingEntity = null;
            SaleEntity? saleEntity = null;
            foreach (var listing in listings)
            {
                var matches = listing.PricePerUnit == sale.PricePerUnit && sale.Hq == listing.IsHq && sale.Quantity == listing.Quantity;
                if (!matches) continue;
                // var diff = Math.Abs(sale.Timestamp.ConvertTimestamp().Subtract(listing.UpdatedAt).TotalSeconds);
                // if (diff > TimeSpan.FromHours(24).TotalSeconds) continue;
                listingEntity = listing;
                listing.Flags = listing.Flags.AddFlag(ListingFlags.Sold);
                db.Update(listing);
                saleEntity = new SaleEntity
                {
                    BuyerName = sale.BuyerName,
                    ListingId = listing.Id,
                    BoughtAt = sale.Timestamp.ConvertTimestamp()
                };
                await db.AddAsync(saleEntity);
                await db.SaveChangesAsync();
                break;
            }

            if (listingEntity != null)
            {
                try
                {
                    var character = await db.Characters.FirstOrDefaultAsync(u => u.Id == listingEntity.RetainerOwnerId);
                    if (character?.SaleNotification != true) return;
                    var client = serviceProvider.GetRequiredService<DiscordSocketClient>();
                    var user = await client.GetUserAsync(character.Id);
                    if (user == null) return;
                    var channel = await user.CreateDMChannelAsync();
                    if (channel == null) return;
                    var item = await db.Items.FirstAsync(i => i.Id == itemId);
                    var cache = serviceProvider.GetRequiredService<CacheService>();
                    cache.Emotes.TryGetValue("gil", out var emote);
                    var rowOne = new ActionRowBuilder()
                        .AddComponent(new ButtonBuilder("Approve", $"sale:approve:{saleEntity!.Id}:true", ButtonStyle.Success).Build())
                        .AddComponent(new ButtonBuilder("Reject", $"sale:reject:{saleEntity.Id}:true", ButtonStyle.Danger).Build())
                        .AddComponent(new ButtonBuilder("Edit Buyer", $"sale:edit:name:{saleEntity.Id}:true").Build());
                    await channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithTitle("Sale Notification")
                        .AddField("Item", item.Name, true)
                        .AddField("Buyer", sale.BuyerName, true)
                        .AddField("Gil", $"{(emote != null ? $"{emote} " : "")}{listingEntity.Quantity * listingEntity.PricePerUnit:N0}", true)
                        .WithColor(Color.Green)
                        .WithTimestamp(sale.Timestamp.ConvertTimestamp())
                        .Build(), components: new ComponentBuilder()
                        .AddRow(rowOne)
                        .Build());
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Unable to send sale notification");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while handling sales");
        }
    }
}