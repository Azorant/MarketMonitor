using Discord;
using Discord.Interactions;
using MarketMonitor.Bot.Jobs;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Bot.Modules;

[Group("retainer", "Retainer commands")]
public class RetainerModule(DatabaseContext db, ApiService api, CacheJob cacheJob, CacheService cacheService, ImageService imageService) : BaseModule(db)
{
    [SlashCommand("setup", "Setup a retainer")]
    public async Task SetupRetainer(string name)
    {
        await DeferAsync(true);
        var character = await GetCharacterAsync();
        if (character == null)
        {
            await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
            return;
        }

        Random random = new Random();
        var itemId = random.Next(2, 7);
        var item = await db.Items.FirstAsync(i => i.Id == itemId);

        var retainer = new RetainerEntity
        {
            Name = name,
            OwnerId = character.Id,
            VerificationItem = item.Id, // Fire Shard
            VerificationPrice = random.Next(512_000, 999_999_999)
        };
        await db.AddAsync(retainer);
        await db.SaveChangesAsync();
        await SendSuccessAsync("Retainer Setup",
            $"Your next step is to list 1 or more `{item.Name}` on the market for `{retainer.VerificationPrice:N0}` each.\nOnce you've done that, come back and run {await GetCommand("retainer", "verify")}");
    }

    [SlashCommand("verify", "Verify your retainers")]
    public async Task VerifyRetainer()
    {
        await DeferAsync(true);
        var character = await GetCharacterAsync();
        if (character == null)
        {
            await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
            return;
        }

        var retainers = await db.Retainers.Where(r => r.OwnerId == character.Id && !r.IsVerified).ToListAsync();
        if (retainers.Count == 0)
        {
            await SendErrorAsync($"You don't have any retainers to verify.\nSetup one with {await GetCommand("retainer", "setup")}");
            return;
        }

        var verified = new List<string>();
        var failed = new List<string>();

        foreach (var retainer in retainers)
        {
            var response = await api.FetchItem(retainer.VerificationItem!.Value, character.DatacenterName);
            var listings = response.Listings.Where(l => l.PricePerUnit == retainer.VerificationPrice && l.RetainerName == retainer.Name).ToList();
            if (listings.Count == 0)
            {
                var item = await db.Items.FirstAsync(i => i.Id == retainer.VerificationItem!.Value);
                failed.Add($"**{retainer.Name}**: `{item.Name}` for `{retainer.VerificationPrice:N0}` ");
                continue;
            }

            retainer.IsVerified = true;
            retainer.VerificationItem = null;
            retainer.VerificationPrice = null;
            retainer.Id = listings.First().RetainerId;
            db.Update(retainer);
            verified.Add(retainer.Name);
        }

        if (verified.Count == 0)
        {
            await SendErrorAsync(
                $"Failed to verify, make sure your {"retainer".Quantize(failed.Count)} {(failed.Count == 1 ? "has" : "have")} the proper item listed on the market.\n{string.Join("\n", failed)}");
            return;
        }

        await db.SaveChangesAsync();
        await cacheJob.PopulateCharacterCache();

        await SendSuccessAsync(
            $"Verified {"retainer".Quantize(verified.Count)} {string.Join(", ", verified)}{(failed.Count == 0 ? string.Empty : $"\n\nFailed to verify {failed.Count} {"retainer".Quantize(failed.Count)}. Make sure they have the proper item listed on the market.\n{string.Join("\n", failed)}")}");
    }

    [SlashCommand("recent-sales", "Show recent sales")]
    public async Task Sales()
    {
        await DeferAsync(true);
        var sales = await db.Sales.Include(s => s.Listing).ThenInclude(l => l.Item).Where(l => l.Listing.RetainerOwnerId == Context.User.Id).OrderByDescending(l => l.BoughtAt)
            .Take(25).ToListAsync();

        var file = await imageService.CreateRecentSales(sales);

        await FollowupWithFileAsync(file);
    }
}