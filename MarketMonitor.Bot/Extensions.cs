namespace MarketMonitor.Bot;

public static class Extensions
{
    public static DateTime ConvertTimestamp(this double timestamp)
        => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
}