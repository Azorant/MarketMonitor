using Discord;
using Discord.Interactions;
using MarketMonitor.Bot.Jobs;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Bot.Modules;

[Group("developer", "Developer Module"), RequireOwner, CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel),
 IntegrationType(ApplicationIntegrationType.UserInstall)]
public class DeveloperModule(DatabaseContext db) : BaseModule(db)
{
    [Group("cache", "update")]
    public class UpdateModule(DatabaseContext db, ApiService api, CacheJob cacheJob) : BaseModule(db)
    {
        [SlashCommand("update", "Update tracking cache")]
        public async Task UpdateCache()
        {
            await DeferAsync();
            var results = await cacheJob.PopulateCharacterCache();
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Cache Updated")
                .WithFields(new EmbedFieldBuilder()
                        .WithName("Characters")
                        .WithValue(results.Item1.ToString("N0"))
                        .WithIsInline(true),
                    new EmbedFieldBuilder()
                        .WithName("Retainers")
                        .WithValue(results.Item2.ToString("N0"))
                        .WithIsInline(true))
                .WithColor(Color.Green)
                .Build());
        }
    }

    [Group("database", "database commands")]
    public class DatabaseModule(DatabaseContext db, ApiService api) : BaseModule(db)
    {
        [SlashCommand("update", "Update database cache")]
        public async Task UpdateDatabase()
        {
            await DeferAsync();
            await api.UpdateCache(Context);
        }

        [SlashCommand("fix", "Fix database tables")]
        public async Task Fix()
        {
            await DeferAsync();
            var sales = await db.Sales.Where(s => s.ListingRetainerName == string.Empty).ToListAsync();
            var fixedSales = 0;
            var failedSales = 0;
            
            foreach (var sale in sales)
            {
                var listing = await db.Listings.FirstOrDefaultAsync(l=>l.Id == sale.ListingId);
                if (listing == null)
                {
                    failedSales++;
                    continue;
                }
                sale.ListingRetainerName = listing.RetainerName;
                db.Update(sale);
                fixedSales++;
            }
            await db.SaveChangesAsync();
            await FollowupAsync($"Fixed {fixedSales:N0} sale rows\nFailed {failedSales:N0} sales rows");
        }
    }
}