using System.Net;
using MarketMonitor.Database;
using MarketMonitor.Bot.HostedServices;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using MarketMonitor.Bot.Jobs;
using MarketMonitor.Bot.Services;
using MarketMonitor.Startup;
using MarketMonitor.Website.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimit;

try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .CreateLogger();
    var builder = WebApplication.CreateBuilder(args);

    var redisConfiguration = Environment.GetEnvironmentVariable("REDIS");
    ArgumentException.ThrowIfNullOrEmpty(redisConfiguration);

    #region Common

    builder.Services
        .AddDbContext<DatabaseContext>(options => DatabaseContextFactory.CreateDbOptions(options), ServiceLifetime.Transient)
        .AddSingleton(ConnectionMultiplexer.Connect(redisConfiguration))
        .AddSerilog();

    #endregion

    #region Polly

    HttpStatusCode[] httpStatusCodesWorthRetrying =
    {
        HttpStatusCode.RequestTimeout, HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    };

    var httpThrottledPolicy = Policy
        .Handle<RateLimitRejectedException>()
        .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
        .CircuitBreakerAsync(1, TimeSpan.FromSeconds(0),
            onBreak: (_, _, _, _) => { },
            onReset: _ => { },
            onHalfOpen: () => { });
    CancellationTokenSource httpThrottlingEndSignal;
    var httpRetryPolicy = Policy
        .Handle<RateLimitRejectedException>()
        .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
        .Or<BrokenCircuitException>()
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3),
            onRetry: (dr, _) =>
            {
                if (dr.Exception is not RateLimitRejectedException dse) return;
                httpThrottledPolicy.Isolate();
                httpThrottlingEndSignal = new CancellationTokenSource(dse.RetryAfter);
                httpThrottlingEndSignal.Token.Register(() => httpThrottledPolicy.Reset());
            });
    /*
     From Universalis docs: There is a rate limit of 25 req/s (50 req/s burst) on the API
     Could probably make the timespan 1 second, but we don't really need that throughput so 10 seconds should be good
     */
    var httpRatelimit = Policy.RateLimitAsync<HttpResponseMessage>(25, TimeSpan.FromSeconds(10), 50);

    builder.Services.AddHttpClient("Universalis")
        .AddPolicyHandler(Policy.WrapAsync(httpRetryPolicy, httpThrottledPolicy, httpRatelimit));
    builder.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();

    #endregion

    #region Bot

    builder.Services
        .AddSingleton(new InteractiveConfig { ReturnAfterSendingPaginator = true })
        .AddSingleton<InteractiveService>()
        .AddSingleton(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        })
        .AddSingleton<DiscordSocketClient>()
        .AddSingleton<InteractionService>(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
        .AddSingleton<UniversalisGeneralWebsocket>()
        .AddTransient<ApiService>()
        .AddTransient<LodestoneService>()
        .AddSingleton<CacheService>()
        .AddSingleton<PrometheusService>()
        .AddTransient<StatusService>()
        .AddTransient<ImageService>()
        .AddHostedService<DiscordClientHost>();

    #endregion

    #region Website

    builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Any, 5123));

    builder.Services
        .AddMudServices()
        .AddRazorComponents()
        .AddInteractiveServerComponents();

    #endregion

    #region Jobs

    builder.Services
        .AddTransient<LogExecutionAttribute>()
        .AddHangfire((provider, configuration) =>
            configuration.UseRedisStorage(provider.GetRequiredService<ConnectionMultiplexer>(), new RedisStorageOptions
                {
                    Prefix = $"{Environment.GetEnvironmentVariable("PREFIX") ?? "marketmonitor"}:hangfire:",
                    Db = 1
                })
                .UseFilter(provider.GetRequiredService<LogExecutionAttribute>())
        )
        .AddHangfireServer()
        .AddSingleton<StatusJob>()
        .AddSingleton<PacketJob>()
        .AddTransient<HealthJob>()
        .AddTransient<CacheJob>()
        .AddTransient<MarketJob>();

    #endregion

    var host = builder.Build();

    if (!host.Environment.IsDevelopment())
    {
        host.UseExceptionHandler("/error", createScopeForErrors: true);
    }

    StaticWebAssetsLoader.UseStaticWebAssets(host.Environment, host.Configuration);

    host.UseAntiforgery();
    host.MapStaticAssets();
    host.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();
    host.UseHangfireDashboard(options: new DashboardOptions
    {
        Authorization = new[] { new DashboardNoAuthorizationFilter() },
        IgnoreAntiforgeryToken = true
    });

    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        await db.ApplyMigrations();
    }

    RecurringJob.AddOrUpdate<StatusJob>("status", x => x.SetStatus(), "0,15,30,45 * * * * *");
    RecurringJob.AddOrUpdate<CacheJob>("cache", x => x.PopulateAll(), "0,30 * * * * *");
    RecurringJob.AddOrUpdate<MarketJob>("market", x => x.UndercutCheck(), "*/10 * * * *");
    RecurringJob.AddOrUpdate<MarketJob>("daily_listing", x => x.DailyListingCheck(), "0 0 * * *");
    RecurringJob.AddOrUpdate<HealthJob>("health", x => x.CheckHealth(), "0,15,30,45 * * * * *");

    host.Run();
}
catch (Exception error)
{
    Log.Error(error, "Error in main");
}
finally
{
    Log.CloseAndFlush();
}