namespace MarketMonitor.Bot.Models.Universalis;

public class MarketBoardDataResponse
{
    public int ItemId { get; set; }
    public double LastUploadTime { get; set; }
    public List<ListingData> Listings { get; set; }
}