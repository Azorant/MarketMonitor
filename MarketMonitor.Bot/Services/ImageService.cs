using Discord;
using Humanizer;
using MarketMonitor.Bot.HostedServices;
using MarketMonitor.Database.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace MarketMonitor.Bot.Services;

public class Row(List<Column> columns)
{
    public List<Column> Columns { get; set; } = columns;
    public float Height => Columns.Max(c => c.Height);
    public int HighestColumnIndex => Columns.FindIndex(col => Math.Abs(col.Height - Height) < 1);
}

public class Column(string text, string? icon = null, HorizontalAlignment alignment = HorizontalAlignment.Left)
{
    public string Text { get; set; } = text;
    public string? Icon { get; set; } = icon;
    public bool HasIcon => !string.IsNullOrEmpty(Icon);
    public float Width { get; set; }
    public float Height { get; set; }
    public HorizontalAlignment Alignment { get; set; } = alignment;
}

public class ImageData
{
    public List<Row> Rows { get; set; } = new();
    public Row HighestRow => Rows.First(row => Math.Abs(row.Height - Rows.Max(r => r.Height)) < 1);
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
        LargeFont = family.CreateFont(48, FontStyle.Regular);
    }

    public async Task<FileAttachment> CreateRecentSales(List<SaleEntity> sales)
    {
        var imageData = new ImageData();

        imageData.Rows.Add(new Row([
            new("Retainer"),
            new("Item", string.Empty),
            new("Qty", alignment: HorizontalAlignment.Center),
            new("Total Gil", string.Empty),
            new("Buyer"),
            new("Bought", alignment: HorizontalAlignment.Center)
        ]));
        imageData.Rows.AddRange(sales.Select(sale => new Row([
            new Column(sale.Listing.RetainerName),
            new Column(sale.Listing.Item.Name, $"https://v2.xivapi.com/api/asset?path={sale.Listing.Item.Icon}&format=png"),
            new Column(sale.Listing.Quantity.ToString(), alignment: HorizontalAlignment.Center),
            new Column((sale.Listing.Quantity * sale.Listing.PricePerUnit).ToString("N0"), "https://v2.xivapi.com/api/asset?path=ui/icon/065000/065002_hr1.tex&format=png"),
            new Column(sale.BuyerName),
            new Column(sale.BoughtAt.Humanize(true), alignment: HorizontalAlignment.Center)
        ])));
        return await BuildImage(imageData);
    }

    public async Task<FileAttachment> CreateRecentPurchases(List<PurchaseEntity> purchases)
    {
        var imageData = new ImageData();
        imageData.Rows.Add(new Row([
            new("Item", string.Empty),
            new("HQ", string.Empty, HorizontalAlignment.Center),
            new("Qty", alignment: HorizontalAlignment.Center),
            new("Total Gil", string.Empty),
            new("World"),
            new("Bought", alignment: HorizontalAlignment.Center)
        ]));
        imageData.Rows.AddRange(purchases.Select(p => new Row([
            new(p.Item.Name, $"https://v2.xivapi.com/api/asset?path={p.Item.Icon}&format=png"),
            new(string.Empty, p.IsHq ? "./Resources/hq.png" : string.Empty),
            new(p.Quantity.ToString(), alignment: HorizontalAlignment.Center),
            new Column((p.Quantity * p.PricePerUnit).ToString("N0"), "https://v2.xivapi.com/api/asset?path=ui/icon/065000/065002_hr1.tex&format=png"),
            new(p.World.Name),
            new Column(p.PurchasedAt.Humanize(true), alignment: HorizontalAlignment.Center)
        ])));

        return await BuildImage(imageData);
    }

    private async Task<FileAttachment> BuildImage(ImageData imageData)
    {
        using Image final = new Image<Bgra32>(1920, 1080);
        final.Mutate(x => x.Fill(Color.Black));

        var basePadding = 30f;
        var iconPadding = 40f;

        for (var i = 0; i < imageData.Rows.Count; i++)
        {
            var row = imageData.Rows[i];
            for (var colIndex = 0; colIndex < row.Columns.Count; colIndex++)
            {
                var column = row.Columns[colIndex];
                var extraPadding = column.HasIcon ? iconPadding : 0;
                var measurement = TextMeasurer.MeasureSize(column.Text, new RichTextOptions(i == 0 ? LargeFont : NormalFont)
                {
                    HorizontalAlignment = column.Alignment,
                    VerticalAlignment = VerticalAlignment.Top,
                    WrappingLength = column.Text.Length > 21 ? (float)final.Width / row.Columns.Count - extraPadding : -1,
                });
                var headerColumn = imageData.Rows[0].Columns[colIndex];
                column.Height = measurement.Height;
                if (measurement.Width > headerColumn.Width) headerColumn.Width = measurement.Width + basePadding + extraPadding;
            }
        }

        imageData.Rows[0].Columns[imageData.HighestRow.HighestColumnIndex].Width +=
            final.Width - imageData.Rows[0].Columns.Sum(x => x.Width) - basePadding * 2 - basePadding / 2 * imageData.Rows[0].Columns.Count;

        var xPadding = (final.Width - imageData.Rows[0].Columns.Sum(x => x.Width)) / imageData.Rows[0].Columns.Count;
        var yPadding = (final.Height - imageData.Rows.Sum(x => x.Height)) / imageData.Rows.Count;
        if (xPadding < basePadding) xPadding = basePadding;
        if (yPadding < basePadding) yPadding = basePadding;
        var x = xPadding;
        var y = basePadding;

        var alignStart = imageData.Rows.Sum(i => i.Height) / final.Height < 0.66;

        var imageCache = new Dictionary<string, Image>();
        var httpClient = new HttpClient();

        for (int rowIndex = 0; rowIndex < imageData.Rows.Count; rowIndex++)
        {
            var row = imageData.Rows[rowIndex];
            if (rowIndex != 0 && final.Height - (y + row.Height) < basePadding) break;
            var tallest = 0f;
            for (int colIndex = 0; colIndex < row.Columns.Count; colIndex++)
            {
                var column = row.Columns[colIndex];
                var headerColumn = imageData.Rows[0].Columns[colIndex];

                Image? icon = null;

                if (column.HasIcon && !imageCache.TryGetValue(column.Icon!, out icon))
                {
                    if (column.Icon!.StartsWith("./Resources"))
                    {
                        icon = (await Image.LoadAsync(column.Icon)).CloneAs<Bgra32>();
                    }
                    else
                    {
                        var httpResult = await httpClient.GetAsync(column.Icon);
                        await using var resultStream = await httpResult.Content.ReadAsStreamAsync();
                        icon = (await Image.LoadAsync(resultStream)).CloneAs<Bgra32>();
                    }

                    imageCache.Add(column.Icon!, icon);
                }

                float extraPadding = column.HasIcon ? iconPadding : 0;
                var xOffset = column.Alignment switch
                {
                    HorizontalAlignment.Center => headerColumn.Width / 2,
                    _ => 0
                };

                var options = new RichTextOptions(rowIndex == 0 ? LargeFont : NormalFont)
                {
                    HorizontalAlignment = column.Alignment,
                    VerticalAlignment = VerticalAlignment.Top,
                    Origin = new PointF(x + extraPadding + xOffset, y),
                };
                var measure = TextMeasurer.MeasureSize(column.Text, options);
                if (measure.Width >= headerColumn.Width)
                {
                    options.WrappingLength = headerColumn.Width;
                    measure = TextMeasurer.MeasureSize(column.Text, options);
                }

                // final.Mutate(c => c.Fill(Color.Purple, new RectangularPolygon(x, y, headerColumn.Width, row.Height)));
                // final.Mutate(c => c.Fill(Color.DarkGoldenrod, new RectangularPolygon(x + extraPadding, y, measure.Width, measure.Height)));

                if (measure.Height > tallest) tallest = measure.Height;
                final.Mutate(c => c.DrawText(options, column.Text, Color.White));
                if (icon != null)
                {
                    var offset = string.IsNullOrEmpty(column.Text) ? headerColumn.Width / 2 - 16 : 0;

                    icon.Mutate(c => c.Resize(new Size(32)));
                    final.Mutate(c => c.DrawImage(icon, new Point((int)(x + offset), (int)y + 8), 1));
                }

                x += headerColumn.Width + xPadding / 1.5f;
            }

            var heightDiff = row.Height - tallest;

            y += (alignStart ? row.Height + basePadding : row.Height + yPadding) - (heightDiff >= basePadding ? heightDiff : 0);
            x = xPadding;
        }

        if (imageData.Rows.Count <= 1)
        {
            var options = new RichTextOptions(LargeFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Origin = new PointF(final.Width / 2, final.Height / 2),
            };
            final.Mutate(c => c.DrawText(options, "No records", Color.White));
        }

        if (DiscordClientHost.IsDebug()) await final.SaveAsPngAsync("./tmp.png");
        Stream stream = new MemoryStream();
        await final.SaveAsync(stream, PngFormat.Instance);
        return new FileAttachment(stream, "sales.png");
    }
}