using Prometheus;
using Serilog;

namespace MarketMonitor.Bot.Services;

public class PrometheusService
{
    private MetricServer Server { get; set; }
    public Gauge Guilds { get; set; }
    public Counter Commands { get; set; }
    public Gauge Latency { get; set; }
    public Gauge TrackedCharacters { get; set; }
    public Gauge TrackedRetainers { get; set; }
    public Gauge TrackedListings { get; set; }
    public Histogram JobExecuted { get; set; }
    public Histogram ImageGenerationLatency { get; set; }

    public PrometheusService()
    {
        Log.Information("Prometheus metrics started");
        Guilds = Metrics.CreateGauge("marketmonitor_guilds", "Guilds bot is in");
        Commands = Metrics.CreateCounter("marketmonitor_commands_total", "Commands ran", labelNames: ["command"]);
        Latency = Metrics.CreateGauge("marketmonitor_latency", "Websocket latency");
        TrackedCharacters = Metrics.CreateGauge("marketmonitor_tracked_characters", "Number of tracked characters");
        TrackedRetainers = Metrics.CreateGauge("marketmonitor_tracked_retainers", "Number of tracked retainers");
        TrackedListings = Metrics.CreateGauge("marketmonitor_tracked_listings", "Number of tracked listings");
        JobExecuted = Metrics.CreateHistogram("marketmonitor_job_executed", "Number of jobs executed and duration", new HistogramConfiguration
        {
            Buckets = Histogram.PowersOfTenDividedBuckets(0, 6, 16),
            LabelNames = ["job"],
        });
        
        ImageGenerationLatency = Metrics.CreateHistogram("marketmonitor_image_generation_latency", "Duration of image gen", new HistogramConfiguration
        {
            Buckets = Histogram.PowersOfTenDividedBuckets(1, 4, 4),
            LabelNames = ["type"],
        });

        Server = new MetricServer(3400);
        Server.Start();
    }
}