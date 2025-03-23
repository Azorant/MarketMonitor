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

    public PrometheusService()
    {
        Log.Information("Prometheus metrics started");
        var prefix = Environment.GetEnvironmentVariable("PREFIX") ?? "marketmonitor";
        Guilds = Metrics.CreateGauge($"{prefix}_guilds", "Guilds bot is in");
        Commands = Metrics.CreateCounter($"{prefix}_commands_total", "Commands ran", labelNames: ["command"]);
        Latency = Metrics.CreateGauge($"{prefix}_latency", "Websocket latency");
        TrackedCharacters = Metrics.CreateGauge($"{prefix}_tracked_characters", "Number of tracked characters");
        TrackedRetainers = Metrics.CreateGauge($"{prefix}_tracked_retainers", "Number of tracked retainers");
        TrackedListings = Metrics.CreateGauge($"{prefix}_tracked_listings", "Number of tracked listings");
        JobExecuted = Metrics.CreateHistogram($"{prefix}_job_executed", "Number of jobs executed and duration", labelNames: ["job"]);

        Server = new MetricServer(3400);
        Server.Start();
    }
}