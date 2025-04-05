using Discord;
using Discord.Interactions;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Bot.Modules;

[Group("market","Market related commands"), CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel), IntegrationType(ApplicationIntegrationType.GuildInstall)]
public class MarketMonitor(DatabaseContext db, ImageService imageService) : BaseModule(db)
{
    [SlashCommand("sales", "Show recent retainer sales")]
    public async Task Sales(bool ephemeral = true)
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
    public async Task RecentPurchases(bool ephemeral = true)
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
}