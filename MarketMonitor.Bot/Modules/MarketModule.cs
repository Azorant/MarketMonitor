using Discord;
using Discord.Interactions;
using Humanizer;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Bot.Modules;

[Group("market", "Market related commands"), CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel),
 IntegrationType(ApplicationIntegrationType.GuildInstall)]
public class MarketModule(DatabaseContext db, ImageService imageService, CacheService cacheService) : BaseModule(db)
{
    [SlashCommand("sales", "Show recent retainer sales")]
    public async Task Sales([Summary(description: "Hide command output")] bool ephemeral = true)
    {
        await DeferAsync(ephemeral);
        var character = await GetVerifiedCharacterAsync();
        if (character == null)
        {
            await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
            return;
        }

        var sales = await db.Sales.Include(s => s.Listing).ThenInclude(l => l.Item).Where(l => l.Listing.RetainerOwnerId == Context.User.Id).OrderByDescending(l => l.BoughtAt)
            .Take(25).ToListAsync();

        await FollowupWithFileAsync(await imageService.CreateRecentSales(sales));
    }

    [SlashCommand("purchases", "Show your recent purchases")]
    public async Task RecentPurchases([Summary(description: "Hide command output")] bool ephemeral = true)
    {
        await DeferAsync(ephemeral);
        var character = await GetVerifiedCharacterAsync();
        if (character == null)
        {
            await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
            return;
        }

        var purchases = await db.Purchases.Include(p => p.Item).Include(p => p.World).Where(p => p.CharacterId == Context.User.Id).OrderByDescending(l => l.PurchasedAt)
            .Take(25).ToListAsync();
        await FollowupWithFileAsync(await imageService.CreateRecentPurchases(purchases));
    }

    [SlashCommand("balance", "Show your gil balance for a specific timeframe")]
    public async Task BalanceGil([MinValue(1), Summary(description: "How many days to calculate")] int timeframe, [Summary(description: "Hide command output")] bool ephemeral = true)
    {
        await DeferAsync(ephemeral);
        var character = await GetVerifiedCharacterAsync();
        if (character == null)
        {
            await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
            return;
        }

        var spent = await db.Purchases
            .Where(p => p.CharacterId == Context.User.Id &&
                        p.PurchasedAt >= DateTime.UtcNow.AddDays(-timeframe))
            .SumAsync(p => p.Quantity * p.PricePerUnit);

        var sold = await db.Sales.Include(s => s.Listing)
            .Where(s => s.Listing.RetainerOwnerId == Context.User.Id &&
                        s.BoughtAt >= DateTime.UtcNow.AddDays(-timeframe))
            .SumAsync(s => s.Listing.Quantity * s.Listing.PricePerUnit);

        cacheService.Emotes.TryGetValue("gil", out var emote);
        var emoji = emote == null ? "" : $"{emote} ";

        await FollowupAsync(embed: new EmbedBuilder()
            .WithTitle("Total Gil")
            .WithDescription($"Showing gil balance for the past {"day".ToQuantity(timeframe)}")
            .WithColor(Color.Teal)
            .AddField("Spent", $"{emoji}{spent:N0}")
            .AddField("Sold", $"{emoji}{sold:N0}")
            .AddField("Net", $"{emoji}{sold - spent:N0}")
            .Build());
    }
}