using Newtonsoft.Json;

namespace MarketMonitor.Bot.Models.Universalis;

public class SubscribePacket(string e, string c)
{
    [JsonProperty("event")]
    public string Event { get; set; } = e;
    [JsonProperty("channel")]
    public string Channel { get; set; } = c;
}