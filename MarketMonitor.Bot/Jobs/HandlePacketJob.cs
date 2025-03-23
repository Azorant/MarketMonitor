using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketMonitor.Bot.Jobs;

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
            }
        }

        await db.SaveChangesAsync();
    }

    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task HandleListingRemove(string retainerId, List<string> listingIds)
    {
        await using var db = serviceProvider.GetRequiredService<DatabaseContext>();
        var retainer = await db.Retainers.AsNoTracking().FirstOrDefaultAsync(r => r.Id == retainerId);
        if (retainer == null) return;
        var listings = await db.Listings.Where(l => listingIds.Contains(l.Id) && !l.IsRemoved).ToListAsync();

        foreach (var listing in listings)
        {
            listing.IsRemoved = true;
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