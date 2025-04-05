using Discord.Interactions;
using MarketMonitor.Bot.Jobs;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;

namespace MarketMonitor.Bot.Modules;

[Group("character", "Character commands")]
public class CharacterModule(DatabaseContext db, LodestoneService lodestone, CacheJob cache) : BaseModule(db)
{
    [SlashCommand("setup", "Setup your character")]
    public async Task SetCharacter([MaxLength(64)] string characterName, [Autocomplete<DatacenterAutocompleteHandler>] string datacenter)
    {
        await DeferAsync(true);
        var existing = await GetCharacterAsync();
        if (existing != null)
        {
            await SendErrorAsync($"You already have a character.{(existing.IsVerified ? $"\nVerify it with {await GetCommand("character", "verify")}" : string.Empty)}");
            return;
        }

        var datacenterEntity = await db.Datacenters.FindAsync(datacenter);
        if (datacenterEntity == null)
        {
            await SendErrorAsync("Unknown datacenter");
            return;
        }

        var searchResponse = await lodestone.SearchCharacterAsync(characterName, datacenterEntity.Name);
        if (searchResponse == null)
        {
            await SendErrorAsync("No character found");
            return;
        }

        var character = new CharacterEntity
        {
            Id = Context.User.Id,
            Name = characterName,
            DatacenterName = datacenterEntity.Name,
            LodestoneId = searchResponse.Id!
        };
        await db.AddAsync(character);
        await db.SaveChangesAsync();
        await SendSuccessAsync(
            $"Character setup.\nYour next step is to add `{character.VerificationValue.ToString()}` to your [lodestone bio](https://na.finalfantasyxiv.com/lodestone/my/setting/profile/).\nOnce you've done that, come back and run {await GetCommand("character", "verify")}");
    }

    [SlashCommand("verify", "Verify your character")]
    public async Task VerifyCharacter()
    {
        await DeferAsync(true);
        var character = await GetVerifiedCharacterAsync();
        if (character == null)
        {
            await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
            return;
        }

        var fetchedCharacter = await lodestone.FetchCharacterAsync(character.LodestoneId);
        if (fetchedCharacter == null)
        {
            await SendErrorAsync("No character found");
            return;
        }

        if (fetchedCharacter.Bio != character.VerificationValue.ToString())
        {
            await SendErrorAsync($"Bio doesn't contain the verification value `{character.VerificationValue.ToString()}`");
            return;
        }

        character.IsVerified = true;
        db.Update(character);
        await db.SaveChangesAsync();
        await cache.PopulateCharacterCache();
        await SendSuccessAsync(
            $"Character verified.\nIf you want to track sale history or get notifications when undercut on the market run {await GetCommand("retainer", "setup")}.");
    }

    [Group("region", "Region commands")]
    public class RegionModule(DatabaseContext db) : BaseModule(db)
    {
        [SlashCommand("notification", "Set the region used for undercut notifications")]
        public async Task NotificationRegion([Autocomplete<WorldAutocompleteHandler>] int? world = null)
        {
            await DeferAsync(true);
            var character = await GetVerifiedCharacterAsync();
            if (character == null)
            {
                await SendErrorAsync($"You don't have a character.\nSetup one with {await GetCommand("character", "setup")}");
                return;
            }

            var worldName = string.Empty;

            if (world == null)
            {
                character.NotificationRegionId = null;
            }
            else
            {
                var worldEntity = await db.Worlds.FindAsync(world);
                if (worldEntity == null)
                {
                    await SendErrorAsync("Unknown world");
                    return;
                }

                worldName = worldEntity.Name;
                character.NotificationRegionId = worldEntity.Id;
            }

            db.Update(character);
            await db.SaveChangesAsync();
            await SendSuccessAsync(world == null
                ? "Region removed. Undercut notifications will now be datacenter wide."
                : $"Undercut notifications will now only be for listings in **{worldName}**.");
        }
    }
}