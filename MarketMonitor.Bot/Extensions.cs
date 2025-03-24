using Humanizer;
using MarketMonitor.Database.Models;

namespace MarketMonitor.Bot;

public static class Extensions
{
    public static DateTime ConvertTimestamp(this double timestamp)
        => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
    public static DateTime SpecifyUtc(this DateTime date) => DateTime.SpecifyKind(date, DateTimeKind.Utc);
    public static string Quantize(this string text, int amount) => text.ToQuantity(amount, ShowQuantityAs.None);
    public static ListingFlags AddFlag(this ListingFlags flags, ListingFlags flag) => flags | flag;
    public static ListingFlags RemoveFlag(this ListingFlags flags, ListingFlags flag) => flags & ~flag;
}