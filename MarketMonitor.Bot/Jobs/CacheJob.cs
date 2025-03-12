using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class CacheJob(DatabaseContext db, CacheService cache)
{
    public async Task PopulateCache()
    {
        try
        {
            var characters = await db.Characters.Select(c => new { Name = c.Name, Id = c.Id }).AsNoTracking().ToListAsync();
            var retainers = await db.Retainers.Select(c => c.Name).AsNoTracking().ToListAsync();

            foreach (var character in characters)
            {
                await cache.SetCharacter(character.Name, character.Id);
            }

            foreach (var retainer in retainers)
            {
                await cache.SetRetainer(retainer);
            }
            
            Log.Information($"Cache populated with {characters.Count:N0} characters {retainers.Count:N0} retainers");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error populating cache");
        }
    }
}