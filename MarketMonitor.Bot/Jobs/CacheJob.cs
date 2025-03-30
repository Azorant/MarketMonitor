using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class CacheJob(DatabaseContext db, CacheService cache, PrometheusService stats)
{
    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task<(int, int)> PopulateCharacterCache()
    {
        var characterCount = 0;
        var retainerCount = 0;
        try
        {
            var characters = await db.Characters.Where(x => x.IsVerified).Select(c => new { Name = c.Name, Id = c.Id }).AsNoTracking().ToListAsync();
            var retainers = await db.Retainers.Where(x => x.IsVerified).Select(c => c.Name).AsNoTracking().ToListAsync();

            foreach (var character in characters)
            {
                await cache.SetCharacter(character.Name, character.Id);
                characterCount++;
            }

            foreach (var retainer in retainers)
            {
                await cache.SetRetainer(retainer);
                retainerCount++;
            }

            Log.Information($"Character cache populated with {characters.Count:N0} characters {retainers.Count:N0} retainers");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error populating character cache");
        }

        stats.TrackedCharacters.Set(characterCount);
        stats.TrackedRetainers.Set(retainerCount);
        return (characterCount, retainerCount);
    }

    [TypeFilter(typeof(LogExecutionAttribute))]
    public async Task PopulateListingCache()
    {
        try
        {
            var listings = await db.Listings
                .Where(l => l.Flags == ListingFlags.None)
                .GroupBy(l => l.ItemId)
                .Select(l => new { ItemId = l.Key, WorldIds = l.Select(x => x.WorldId) })
                .AsNoTracking()
                .ToListAsync();

            var count = 0;
            foreach (var listing in listings)
            {
                var worldIds = listing.WorldIds.Distinct();
                foreach (var worldId in worldIds)
                {
                    await cache.SetListing(listing.ItemId, worldId);
                    count++;
                }
            }

            Log.Information($"Listing cache populated with {count:N0} listings");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error populating listing cache");
        }
    }
}