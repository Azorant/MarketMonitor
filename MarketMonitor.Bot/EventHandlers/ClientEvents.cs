using Discord;
using Discord.WebSocket;
using MarketMonitor.Bot.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MarketMonitor.Bot.EventHandlers;

public class ClientEvents(IServiceProvider serviceProvider)
{
    private readonly DiscordSocketClient client = serviceProvider.GetRequiredService<DiscordSocketClient>();
    private readonly PrometheusService prometheusService = serviceProvider.GetRequiredService<PrometheusService>();
    private readonly StatusService statusService = serviceProvider.GetRequiredService<StatusService>();
    
    public Task OnGuildJoined(SocketGuild guild)
    {
        Task.Run(async () =>
        {
            prometheusService.Guilds.Inc();
            if (!ulong.TryParse(Environment.GetEnvironmentVariable("GUILD_CHANNEL"), out var channelId)) return;
            if (client.GetChannel(channelId) is not SocketTextChannel channel || channel.GetChannelType() != ChannelType.Text) return;

            var user = await client.GetUserAsync(guild.OwnerId);
            var owner = user == null
                ? $"**Owner ID:** {guild.OwnerId}"
                : $"**Owner:** {Format.UsernameAndDiscriminator(user, false)}\n**Owner ID:** {user.Id}";

            await channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("Joined guild")
                .WithDescription($"**Name:** {guild.Name}\n**ID:** {guild.Id}\n{owner}\n**Members:** {guild.MemberCount}\n**Created:** {guild.CreatedAt:f}")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .WithThumbnailUrl(guild.IconUrl).Build());
        }).ContinueWith(t =>
        {
            if (t.Exception != null) Log.Error(t.Exception, "Error while logging guild join");
        });
        return Task.CompletedTask;
    }

    public Task OnGuildLeft(SocketGuild guild)
    {
        Task.Run(async () =>
        {
            prometheusService.Guilds.Dec();
            if (!ulong.TryParse(Environment.GetEnvironmentVariable("GUILD_CHANNEL"), out var channelId)) return;
            if (client.GetChannel(channelId) is not SocketTextChannel channel || channel.GetChannelType() != ChannelType.Text) return;

            var user = await client.GetUserAsync(guild.OwnerId);
            var owner = user == null
                ? $"**Owner ID:** {guild.OwnerId}"
                : $"**Owner:** {Format.UsernameAndDiscriminator(user, false)}\n**Owner ID:** {user.Id}";

            await channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("Left guild")
                .WithDescription($"**Name:** {guild.Name}\n**ID:** {guild.Id}\n{owner}\n**Members:** {guild.MemberCount}\n**Created:** {guild.CreatedAt:f}")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .WithThumbnailUrl(guild.IconUrl).Build());
        }).ContinueWith(t =>
        {
            if (t.Exception != null) Log.Error(t.Exception, "Error while logging guild leave");
        });
        return Task.CompletedTask;
    }

    public Task OnClientReady()
    {
        Task.Run(async () =>
        {
            await statusService.SendUpdate("Client Connected", Color.Green);
        }).ContinueWith(t =>
        {
            if (t.Exception != null) Log.Error(t.Exception, "Error while logging client ready");
        });
        return Task.CompletedTask;
    }

    public Task OnClientDisconnected(Exception exception)
    {
        Task.Run(async () =>
        {
            await statusService.SendUpdate("Client Disconnected", exception.Message, Color.Gold);
        }).ContinueWith(t =>
        {
            if (t.Exception != null) Log.Error(t.Exception, "Error while logging client disconnect");
        });
        return Task.CompletedTask;
    }
}