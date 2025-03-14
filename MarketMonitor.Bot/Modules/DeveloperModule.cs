using Discord;
using Discord.Interactions;
using MarketMonitor.Bot.Jobs;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;

namespace MarketMonitor.Bot.Modules;

[Group("developer", "Developer Module"), RequireOwner]
public class DeveloperModule(DatabaseContext db) : BaseModule(db)
{
    [Group("update", "update")]
    public class UpdateModule(DatabaseContext db, ApiService api, CacheJob cacheJob) : BaseModule(db)
    {
        [SlashCommand("database", "Update database cache")]
        public async Task UpdateDatabase()
        {
            await DeferAsync();
            await api.UpdateCache(Context);
        }

        [SlashCommand("cache", "Update tracking cache")]
        public async Task UpdateCache()
        {
            await DeferAsync();
            var results = await cacheJob.PopulateCache();
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
}