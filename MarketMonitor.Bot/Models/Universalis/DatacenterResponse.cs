using System.Text.Json.Serialization;

namespace MarketMonitor.Bot.Models.Universalis;

public class DatacenterResponse
{
    public string Name { get; set; }
    public string Region { get; set; }
    public List<int> Worlds { get; set; }
}