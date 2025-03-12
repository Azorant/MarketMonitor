﻿using Discord.Interactions;
using MarketMonitor.Bot.Services;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;

namespace MarketMonitor.Bot.Modules;

[Group("character", "Character commands")]
public class CharacterModule(DatabaseContext db, LodestoneService lodestone, CacheService cache) : BaseModule(db)
{
    [SlashCommand("setup", "Setup your character")]
    public async Task SetCharacter([MaxLength(64)] string characterName, [Autocomplete<DatacenterAutocompleteHandler>] string datacenter)
    {
        await DeferAsync(true);
        var existing = await GetCharacterAsync();
        if (existing != null)
        {
            await SendErrorAsync("You already have a character");
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
        var character = await GetCharacterAsync();
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
        await cache.SetCharacter(character.Name, character.Id);
        await SendSuccessAsync("Character verified.\nIf you want to track sale history or get notifications when undercut on the market run {command}."); // TODO: Update command
    }
}