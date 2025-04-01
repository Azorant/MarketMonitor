using Discord;
using Humanizer;
using MarketMonitor.Bot.HostedServices;
using MarketMonitor.Database.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace MarketMonitor.Bot.Services;

public class Row(List<Column> columns, float measurement = 0)
{
    public List<Column> Columns { get; set; } = columns;
    public float Height { get; set; } = measurement;
}

public class Column(string text, string? icon = null)
{
    public string Text { get; set; } = text;
    public string? Icon { get; set; } = icon;
    public bool HasIcon => !string.IsNullOrEmpty(Icon);
    public float Width { get; set; }
}

public class ImageService
{
    private Font LargeFont { get; set; }
    private Font NormalFont { get; set; }

    public ImageService()
    {
        var fontCollection = new FontCollection();
        fontCollection.AddSystemFonts();
        var family = fontCollection.Get("Open Sans");
        NormalFont = family.CreateFont(42, FontStyle.Regular);
        LargeFont = family.CreateFont(52, FontStyle.Regular);
    }

    public async Task<FileAttachment> CreateRecentSales(List<SaleEntity> sales)
    {
        using Image final = new Image<Bgra32>(1920, 1080);
        final.Mutate(x => x.Fill(Color.Black));

        var basePadding = 30f;

        var rows = new List<Row>();
        rows.Add(new Row([new("Retainer"), new("Item", string.Empty), new("Quantity"), new("Gil", string.Empty), new("Buyer"), new("Bought")]));
        rows.AddRange(sales.Select(sale => new Row([
            new Column(sale.Listing.RetainerName),
            new Column(sale.Listing.Item.Name, $"https://v2.xivapi.com/api/asset?path={sale.Listing.Item.Icon}&format=png"),
            new Column(sale.Listing.Quantity.ToString()),
            new Column((sale.Listing.Quantity * sale.Listing.PricePerUnit).ToString("N0"), "https://v2.xivapi.com/api/asset?path=ui/icon/065000/065002_hr1.tex&format=png"),
            new Column(sale.BuyerName),
            new Column(sale.BoughtAt.Humanize(true))
        ])));

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            for (var colIndex = 0; colIndex < row.Columns.Count; colIndex++)
            {
                var column = row.Columns[colIndex];
                var measurement = TextMeasurer.MeasureSize(column.Text, new RichTextOptions(i == 0 ? LargeFont : NormalFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    WrappingLength = column.Text.Length > 21 ? (float)final.Width / row.Columns.Count : -1,
                });
                var headerColumn = rows[0].Columns[colIndex];
                if (measurement.Height > row.Height) row.Height = measurement.Height;
                if (measurement.Width > headerColumn.Width) headerColumn.Width = measurement.Width + basePadding;
            }
        }

        var room = (final.Width - rows[0].Columns.Sum(x => x.Width) - basePadding * 2) / rows[0].Columns.Count;

        foreach (var column in rows.First().Columns)
        {
            column.Width += room / 1.5f;
        }

        var xPadding = (final.Width - rows[0].Columns.Sum(x => x.Width)) / rows[0].Columns.Count;
        var yPadding = (final.Height - rows.Sum(x => x.Height)) / rows.Count;
        if (xPadding < basePadding) xPadding = basePadding;
        if (yPadding < basePadding) yPadding = basePadding;
        var x = xPadding;
        var y = yPadding;

        var alignStart = rows.Sum(i => i.Height) / final.Height < 0.66;

        var imageCache = new Dictionary<string, Image>();
        var httpClient = new HttpClient();

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (final.Height - (y + row.Height) < basePadding) break;
            var tallest = 0f;
            for (int colIndex = 0; colIndex < row.Columns.Count; colIndex++)
            {
                var column = row.Columns[colIndex];
                var headerColumn = rows[0].Columns[colIndex];

                Image? icon = null;

                if (column.HasIcon && !imageCache.TryGetValue(column.Icon!, out icon))
                {
                    var httpResult = await httpClient.GetAsync(column.Icon);
                    await using var resultStream = await httpResult.Content.ReadAsStreamAsync();
                    icon = (await Image.LoadAsync(resultStream)).CloneAs<Bgra32>();
                    imageCache.Add(column.Icon!, icon);
                }

                float extraPadding = column.HasIcon ? 40 : 0;
                var options = new RichTextOptions(rowIndex == 0 ? LargeFont : NormalFont)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Origin = new PointF(x + extraPadding, y),
                    WrappingLength = headerColumn.Width - extraPadding
                };
                var measure = TextMeasurer.MeasureSize(column.Text, options);
                // final.Mutate(c => c.Fill(Color.Purple, new RectangularPolygon(x, y, headerColumn.Width, row.Height)));
                // final.Mutate(c => c.Fill(Color.DarkGoldenrod, new RectangularPolygon(x, y, measure.Width, measure.Height)));
                if (measure.Height > tallest) tallest = measure.Height;
                final.Mutate(c => c.DrawText(options, column.Text, Color.White));
                if (icon != null)
                {
                    icon.Mutate(c => c.Resize(new Size(32)));
                    final.Mutate(c => c.DrawImage(icon, new Point((int)x, (int)y + 8), 1));
                }

                x += headerColumn.Width + xPadding / 1.5f;
            }

            var heightDiff = row.Height - tallest;

            y += (alignStart ? row.Height + basePadding : row.Height + yPadding) - (heightDiff >= basePadding ? heightDiff : 0);
            x = xPadding;
        }

        if (DiscordClientHost.IsDebug()) await final.SaveAsPngAsync("./tmp.png");
        Stream stream = new MemoryStream();
        await final.SaveAsync(stream, PngFormat.Instance);
        return new FileAttachment(stream, "sales.png");
    }
}