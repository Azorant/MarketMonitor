using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using MarketMonitor.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketMonitor.Bot.Jobs;

public struct RemovedListing(string id, double timestamp)
{
    public string Id { get; set; } = id;
    public double Timestamp { get; set; } = timestamp;
}

public class HandlePacketJob(IServiceProvider serviceProvider)
{
    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task HandleListingAdd(string retainerId, int itemId, int worldId, List<ListingData> listings)
    {
        await using var db = serviceProvider.GetRequiredService<DatabaseContext>();
        var retainer = await db.Retainers.AsNoTracking().FirstOrDefaultAsync(r => r.Id == retainerId);
        if (retainer == null) return;

        var existingListings = await db.Listings.Where(l => listings.Select(e => e.ListingId).Contains(l.Id)).ToListAsync();

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
                    IsHq = listing.Hq
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

                existing.UpdatedAt = listing.LastReviewTime.ConvertTimestamp();

                db.Update(existing);
            }
        }

        await db.SaveChangesAsync();
    }

    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task HandleListingRemove(string retainerId, List<RemovedListing> removedListings)
    {
        await using var db = serviceProvider.GetRequiredService<DatabaseContext>();
        var retainer = await db.Retainers.AsNoTracking().FirstOrDefaultAsync(r => r.Id == retainerId);
        if (retainer == null) return;
        var ids = removedListings.Select(l => l.Id).ToList();
        var listings = await db.Listings.Where(l => ids.Contains(l.Id) && !l.Flags.HasFlag(ListingFlags.Removed)).ToListAsync();

        foreach (var listing in listings)
        {
            listing.Flags = listing.Flags.AddFlag(ListingFlags.Removed);
            listing.UpdatedAt = removedListings.First(l => l.Id == listing.Id).Timestamp.ConvertTimestamp();
            db.Update(listing);
        }

        await db.SaveChangesAsync();
    }

    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task HandleSaleAdd(ulong buyerId, int itemId, int worldId, List<SaleData> sales)
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
}