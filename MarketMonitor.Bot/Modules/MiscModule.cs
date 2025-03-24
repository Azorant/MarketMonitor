using System.Reflection;
using Discord;
using Discord.Interactions;
using MarketMonitor.Bot.HostedServices;
using MarketMonitor.Bot.Services;
using Microsoft.EntityFrameworkCore;

namespace MarketMonitor.Bot.Modules;

public class MiscModule(ApiService api) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("about", "Information about the bot")]
    public async Task AboutCommand()
    {
        var library = Assembly.GetAssembly(typeof(InteractionModuleBase))!.GetName();
        var self = Context.Client.GetUser(160168328520794112);
        var embed = new EmbedBuilder()
            .WithAuthor(Context.Client.CurrentUser.Username, Context.Client.CurrentUser.GetAvatarUrl())
            .AddField("Guilds", Context.Client.Guilds.Count.ToString("N0"), true)
            .AddField("Users", Context.Client.Guilds.Select(guild => guild.MemberCount).Sum().ToString("N0"), true)
            .AddField("Library", $"Discord.Net {library.Version!.ToString()}", true)
            .AddField("Developer", $"{Format.UsernameAndDiscriminator(self, false)}", true)
            .AddField("Links",
                $"[GitHub](https://github.com/Azorant)\n[Support](https://discord.gg/{Environment.GetEnvironmentVariable("DISCORD_INVITE")})\n[Ko-fi](https://ko-fi.com/azorant)",
                true)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .WithFooter(Format.UsernameAndDiscriminator(Context.User, false), Context.User.GetAvatarUrl())
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("invite", "Invite the bot")]
    public async Task InviteCommand()
        => await RespondAsync(
            $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot%20applications.commands");

    [SlashCommand("help", "Commands")]
    public async Task HelpCommand()
    {
        var commands = await (DiscordClientHost.IsDebug() ? Context.Guild.GetApplicationCommandsAsync() : Context.Client.GetGlobalApplicationCommandsAsync());

        var embed = new EmbedBuilder()
            .WithTitle("Commands")
            .WithColor(Color.Blue)
            .WithDescription(string.Join("\n", commands.Select(c => $"</{c.Name}:{c.Id}>")))
            .WithFooter(Format.UsernameAndDiscriminator(Context.User, false), Context.User.GetAvatarUrl())
            .Build();

        await RespondAsync(embed: embed);
    }
}