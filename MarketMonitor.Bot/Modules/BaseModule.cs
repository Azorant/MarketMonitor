using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MarketMonitor.Bot.HostedServices;
using MarketMonitor.Database;
using MarketMonitor.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Bot.Modules;

public class BaseModule(DatabaseContext db) : InteractionModuleBase<SocketInteractionContext>
{
    internal Task<CharacterEntity?> GetCharacterAsync()
    {
        return db.Characters.FirstOrDefaultAsync(c => c.Id == Context.User.Id);
    }

    internal Task<CharacterEntity?> GetVerifiedCharacterAsync()
    {
        return db.Characters.FirstOrDefaultAsync(c => c.Id == Context.User.Id && c.IsVerified);
    }

    internal async Task SendErrorAsync(string error, string title = "Error", bool ephemeral = false)
    {
        var embed = new EmbedBuilder()
            .WithTitle($":warning: {title}")
            .WithDescription(error)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .WithColor(Color.Gold)
            .Build();
        if (Context.Interaction.HasResponded)
        {
            await FollowupAsync(embed: embed, ephemeral: ephemeral);
        }
        else
        {
            await RespondAsync(embed: embed, ephemeral: ephemeral);
        }
    }

    internal Task SendSuccessAsync(string description, bool modify = false) => SendSuccessAsync("Success", description, modify);

    internal async Task SendSuccessAsync(string title, string description, bool modify = false)
    {
        var embed = new EmbedBuilder()
            .WithTitle(":white_check_mark: Success")
            .WithDescription(description)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .WithColor(Color.Green)
            .Build();
        if (Context.Interaction.HasResponded)
        {
            if (modify)
                await ModifyOriginalResponseAsync(m => m.Embed = embed);
            else
                await FollowupAsync(embed: embed);
        }
        else
        {
            await RespondAsync(embed: embed);
        }
    }

    private Task<IReadOnlyCollection<SocketApplicationCommand>> GetCommandsAsync() =>
        DiscordClientHost.IsDebug() ? Context.Guild.GetApplicationCommandsAsync() : Context.Client.GetGlobalApplicationCommandsAsync();
    internal async Task<string> GetCommand(string name, string? subcommand = null)
    {
        var commands = await GetCommandsAsync();
        var command = commands.FirstOrDefault(c => c.Name == name);
        return $"{(command != null ? "<" : "")}/{name}{(string.IsNullOrEmpty(subcommand) ? "" : $" {subcommand}")}{(command != null ? $":{command.Id}>" : "")}";
    }
}

public class WorldAutocompleteHandler(DatabaseContext db) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var text = (autocompleteInteraction.Data.Current.Value as string)?.ToLower();

        var worlds = await db.Worlds.Where(w => string.IsNullOrEmpty(text) || w.Name.ToLower().Contains(text)).OrderBy(w => w.Id).Take(25).AsNoTracking().ToListAsync();

        return AutocompletionResult.FromSuccess(worlds.Select(w =>
            new AutocompleteResult(w.Name, w.Id.ToString())));
    }
}

public class DatacenterAutocompleteHandler(DatabaseContext db) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var text = (autocompleteInteraction.Data.Current.Value as string)?.ToLower();

        var dcs = await db.Datacenters.Where(d => string.IsNullOrEmpty(text) || d.Name.ToLower().Contains(text)).OrderBy(d => d.Name).Take(25).AsNoTracking().ToListAsync();

        return AutocompletionResult.FromSuccess(dcs.Select(d =>
            new AutocompleteResult(d.Name, d.Name)));
    }
}