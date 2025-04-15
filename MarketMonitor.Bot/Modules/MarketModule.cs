using Discord;
using Discord.Interactions;
using Humanizer;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using MarketMonitor.Database.Models;
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


    [SlashCommand("listings", "Show your listings")]
    public async Task ShowListings([Summary(description: "Hide command output")] bool ephemeral = true)
    {
        await DeferAsync(ephemeral);
        var character = await GetVerifiedCharacterAsync();
        if (character == null)
        {
            await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
            return;
        }

        var listings = await db.Listings
            .Include(l => l.Item)
            .Where(l => l.RetainerOwnerId == Context.User.Id && l.Flags == ListingFlags.None)
            .OrderByDescending(l => l.UpdatedAt)
            .Take(25).ToListAsync();
        await FollowupWithFileAsync(await imageService.CreateListings(listings));
    }

    [SlashCommand("balance", "Show your gil balance for a specific timeframe")]
    public async Task BalanceGil(
        [MinValue(1), Summary(description: "How many days to calculate")]
        int timeframe,
        [Summary(description: "Hide command output")]
        bool ephemeral = true)
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
            .SumAsync(p => p.Quantity * p.PricePerUnit * 1.05);

        var sold = await db.Sales.Include(s => s.Listing)
            .Where(s => s.Listing.RetainerOwnerId == Context.User.Id &&
                        s.BoughtAt >= DateTime.UtcNow.AddDays(-timeframe))
            .SumAsync(s => s.Listing.Quantity * s.Listing.PricePerUnit * s.Listing.TaxRate);

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

    [SlashCommand("verify", "Verify market sales")]
    public async Task VerifyCommand()
    {
        await DeferAsync(true);
        var character = await GetVerifiedCharacterAsync();
        if (character == null)
        {
            await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
            return;
        }

        await SendSaleVerification(await GetNextSale());
    }

    [ComponentInteraction("sale:approve:*", true)]
    public async Task ApproveSaleButton(int saleId)
    {
        await DeferAsync(true);
        var sale = await db.Sales.Include(s => s.Listing).ThenInclude(l => l.Item).FirstOrDefaultAsync(s => s.Id == saleId && s.Listing.RetainerOwnerId == Context.User.Id);
        if (sale == null) return;
        sale.Listing.Flags = sale.Listing.Flags.AddFlag(ListingFlags.Confirmed);
        db.Update(sale.Listing);
        await db.SaveChangesAsync();
        await SendSaleVerification(await GetNextSale(), 0);
    }

    [ComponentInteraction("sale:reject:*", true)]
    public async Task RejectSaleButton(int saleId)
    {
        await DeferAsync(true);
        var sale = await db.Sales.Include(s => s.Listing).ThenInclude(l => l.Item).FirstOrDefaultAsync(s => s.Id == saleId && s.Listing.RetainerOwnerId == Context.User.Id);
        if (sale == null) return;
        sale.Listing.Flags = sale.Listing.Flags.RemoveFlag(ListingFlags.Sold);
        db.Update(sale.Listing);
        db.Remove(sale);
        await db.SaveChangesAsync();
        await SendSaleVerification(await GetNextSale(), 1);
    }

    [ComponentInteraction("sale:edit:name:*", true)]
    public async Task EditBuyerNameButton(int saleId)
    {
        var sale = await db.Sales.Include(s => s.Listing).ThenInclude(l => l.Item).FirstOrDefaultAsync(s => s.Id == saleId && s.Listing.RetainerOwnerId == Context.User.Id);
        if (sale == null) return;
        await RespondWithModalAsync<BuyerNameModal>($"edit:name:modal:{sale.Id}", modifyModal: options => options.UpdateTextInput("name", input => input.Value = sale.BuyerName));
    }

    [ModalInteraction("edit:name:modal:*", true)]
    public async Task EditBuyerNameModal(int saleId, BuyerNameModal modal)
    {
        await DeferAsync(true);
        var sale = await db.Sales.Include(s => s.Listing).ThenInclude(l => l.Item).FirstOrDefaultAsync(s => s.Id == saleId && s.Listing.RetainerOwnerId == Context.User.Id);
        if (sale == null) return;
        sale.BuyerName = modal.Name;
        db.Update(sale);
        await db.SaveChangesAsync();
        await SendSaleVerification(sale);
    }

    private async Task SendSaleVerification(SaleEntity? sale, int previous = -1)
    {
        if (sale == null)
        {
            await SendErrorAsync("You have no sales left to verify.", "Whoa", true);
            return;
        }

        cacheService.Emotes.TryGetValue("gil", out var emote);
        var embed = new EmbedBuilder()
            .WithTitle("Verify Sale")
            .AddField("Item", sale.Listing.Item.Name)
            .AddField("Quantity", sale.Listing.Quantity, true)
            .AddField("PPU", $"{emote} {sale.Listing.PricePerUnit:N0}", true)
            .AddField("Total", $"{emote} {sale.Listing.Total:N0}", true)
            .AddField("Buyer", sale.BuyerName, true)
            .AddField("Bought At",
                $"{TimestampTag.FormatFromDateTime(sale.BoughtAt.SpecifyUtc(), TimestampTagStyles.ShortDateTime)} ({TimestampTag.FormatFromDateTime(sale.BoughtAt.SpecifyUtc(), TimestampTagStyles.Relative)})",
                true)
            .AddField("Listing ID", sale.Listing.Id)
            .WithThumbnailUrl($"https://v2.xivapi.com/api/asset?path={sale.Listing.Item.IconPath}&format=png")
            .WithColor(Color.DarkTeal)
            .Build();

        var rowOne = new ActionRowBuilder()
            .AddComponent(new ButtonBuilder("Approve", $"sale:approve:{sale.Id}", ButtonStyle.Success).Build())
            .AddComponent(new ButtonBuilder("Reject", $"sale:reject:{sale.Id}", ButtonStyle.Danger).Build());
        var rowTwo = new ActionRowBuilder()
            .AddComponent(new ButtonBuilder("Edit Buyer", $"sale:edit:name:{sale.Id}").Build());

        var components = new ComponentBuilder()
            .AddRow(rowOne)
            .AddRow(rowTwo)
            .Build();

        if (Context.Interaction.HasResponded)
        {
            if (previous > -1)
            {
                await ModifyOriginalResponseAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle(previous == 0 ? "Sale approved" : "Sale rejected")
                        .WithColor(previous == 0 ? Color.Green : Color.Red)
                        .Build();
                    m.Components = null;
                });
                await Task.Delay(4000);
            }

            await ModifyOriginalResponseAsync(m =>
            {
                m.Embed = embed;
                m.Components = components;
            });
        }
        else
        {
            await RespondAsync(embed: embed, components: components, ephemeral: true);
        }
    }

    private Task<SaleEntity?> GetNextSale() => db.Sales
        .Include(s => s.Listing)
        .ThenInclude(l => l.Item)
        .Where(s => s.Listing.Flags == ListingFlags.Removed.AddFlag(ListingFlags.Sold) && s.Listing.RetainerOwnerId == Context.User.Id)
        .OrderBy(s => s.BoughtAt)
        .FirstOrDefaultAsync();


    public class BuyerNameModal : IModal
    {
        public string Title => "Edit Buyer";

        [InputLabel("Name")]
        [ModalTextInput("name", TextInputStyle.Short)]
        public required string Name { get; set; }
    }
}