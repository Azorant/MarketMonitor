using System.Text.Json.Serialization;
using MarketMonitor.Database.Models;

namespace MarketMonitor.Bot.Models.Universalis;

public class CityTaxRates
{
    [JsonPropertyName("Limsa Lominsa")]
    public int LimsaLominsa { get; set; }

    [JsonPropertyName("Gridania")]
    public int Gridania { get; set; }

    [JsonPropertyName("Ul'dah")]
    public int Uldah { get; set; }

    [JsonPropertyName("Ishgard")]
    public int Ishgard { get; set; }

    [JsonPropertyName("Kugane")]
    public int Kugane { get; set; }

    [JsonPropertyName("Crystarium")]
    public int Crystarium { get; set; }

    [JsonPropertyName("Old Sharlayan")]
    public int OldSharlayan { get; set; }

    [JsonPropertyName("Tuliyollal")]
    public int Tuliyollal { get; set; }

    public int GetCityRate(RetainerCity city) => city switch
    {
        RetainerCity.LimsaLominsa => LimsaLominsa,
        RetainerCity.Gridania => Gridania,
        RetainerCity.Uldah => Uldah,
        RetainerCity.Ishgard => Ishgard,
        RetainerCity.Kugane => Kugane,
        RetainerCity.Crystarium => Crystarium,
        RetainerCity.OldSharlayan => OldSharlayan,
        RetainerCity.Tuliyollal => Tuliyollal,
        _ => 5
    };

    public TimeSpan ResetsIn()
    {
        var now = DateTime.UtcNow;
        var time = now
            .AddDays(((int)DayOfWeek.Saturday - (int)DateTime.UtcNow.DayOfWeek + 7) % 7)
            .AddHours(8 - now.Hour)
            .AddMinutes(-now.Minute)
            .AddSeconds(-now.Second)
            .AddMilliseconds(-now.Millisecond)
            .Subtract(now);
        if (time.TotalSeconds < 0) time = time.Add(TimeSpan.FromDays(7)); // Only happens on saturday
        return time;
    }
}