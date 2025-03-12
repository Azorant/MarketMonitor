using Newtonsoft.Json;

namespace MarketMonitor.Bot.Models.Universalis;

public class DataPacket
{
    public string Event { get; set; }
    public int World { get; set; }
    public int Item { get; set; }
    public List<ListingData>? Listings { get; set; }
    public List<SaleData>? Sales { get; set; }
}

public class ListingData
{
    public double LastReviewTime { get; set; }
    public int PricePerUnit { get; set; }
    public int Quantity { get; set; }
    public bool Hq { get; set; }
    public string ListingId { get; set; }
    public string RetainerId { get; set; }
    public string RetainerName { get; set; }
}

public class SaleData
{
    public bool Hq { get; set; }
    public int PricePerUnit { get; set; }
    public int Quantity { get; set; }
    public double Timestamp { get; set; }
    public string BuyerName { get; set; }
}