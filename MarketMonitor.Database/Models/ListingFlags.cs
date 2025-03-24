namespace MarketMonitor.Database.Models;

[Flags]
public enum ListingFlags
{
    None = 0,
    Removed = 1 << 1,
    Sold = 1 << 2,
    Confirmed = 1 << 3
}