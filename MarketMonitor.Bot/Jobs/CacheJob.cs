using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace MarketMonitor.Bot.Jobs;

public class CacheJob(DatabaseContext db, CacheService cache, PrometheusService stats)
{
    public async Task<(int, int)> PopulateCache()
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

            Log.Information($"Cache populated with {characters.Count:N0} characters {retainers.Count:N0} retainers");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error populating cache");
        }

        stats.TrackedCharacters.Set(characterCount);
        stats.TrackedRetainers.Set(retainerCount);
        return (characterCount, retainerCount);
    }
}