using Hangfire;
using MarketMonitor.Bot.Models.Universalis;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using MarketMonitor.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class HandleSaleJob(IServiceProvider serviceProvider)
{
    [DisableConcurrentExecution("sales", 60)]
    public async Task Handle(int itemId, int worldId, SaleData sale)
    {
        try
        {
            var db = serviceProvider.GetRequiredService<DatabaseContext>();
            var listings = await db.Listings.Where(l => l.ItemId == itemId && l.WorldId == worldId && l.Flags == ListingFlags.Removed).ToListAsync();

            foreach (var listing in listings)
            {
                var matches = listing.PricePerUnit == sale.PricePerUnit && sale.Hq == listing.IsHq && sale.Quantity == listing.Quantity;
                if (!matches) continue;
                var diff = Math.Abs(sale.Timestamp.ConvertTimestamp().Subtract(listing.UpdatedAt).TotalSeconds);
                if (diff > TimeSpan.FromHours(24).TotalSeconds) continue;
                listing.Flags = listing.Flags.AddFlag(ListingFlags.Sold);
                db.Update(listing);
                await db.AddAsync(new SaleEntity
                {
                    BuyerName = sale.BuyerName,
                    ListingId = listing.Id,
                    BoughtAt = sale.Timestamp.ConvertTimestamp()
                });
                await db.SaveChangesAsync();
                break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while handling sales");
        }
    }
}