using Discord;
using Discord.WebSocket;
using Serilog;

namespace MarketMonitor.Bot.Services;

public class StatusService(DiscordSocketClient client)
{
    public async Task SendUpdate(string title, Color color) => await SendUpdate(title, null, color);

    public async Task SendUpdate(string title, string? description, Color color)
    {
        await SendEmbed(new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithCurrentTimestamp());
    }
    
    public async Task SendEmbed(EmbedBuilder builder)
    {
        try
        {

            if (!ulong.TryParse(Environment.GetEnvironmentVariable("LOG_CHANNEL"), out var channelId)) return;
            if (client.GetChannel(channelId) is not SocketTextChannel channel || channel.GetChannelType() != ChannelType.Text) return;

            await channel.SendMessageAsync(embed: builder.Build());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error sending status message");
        }
    }
}