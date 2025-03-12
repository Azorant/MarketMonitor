using System.Text.Json.Serialization;

namespace MarketMonitor.Bot.Models.XIVAPI;

public class ItemResponse
{
    public List<ItemData> Rows { get; set; }
}

public class ItemData
{
    [JsonPropertyName("row_id")]
    public int Id { get; set; }
    public ItemDataFields Fields { get; set; }
}

public class ItemDataFields
{
    public ItemDataIcon Icon { get; set; }
    public string Name { get; set; }
}

public class ItemDataIcon
{
    [JsonPropertyName("path_hr1")]
    public string Path { get; set; }
}
