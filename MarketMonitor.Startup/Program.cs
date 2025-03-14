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
        .AddSingleton<UniversalisWebsocket>()
        .AddTransient<ApiService>()
        .AddTransient<LodestoneService>()
        .AddSingleton<CacheService>()
        .AddSingleton<PrometheusService>()
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

    builder.Services.AddHangfire((provider, configuration) => configuration.UseRedisStorage(provider.GetRequiredService<ConnectionMultiplexer>(), new RedisStorageOptions
        {
            Prefix = $"{Environment.GetEnvironmentVariable("PREFIX") ?? "marketmonitor"}:hangfire:"
        }))
        .AddHangfireServer()
        .AddSingleton<StatusJob>()
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

    RecurringJob.AddOrUpdate<StatusJob>("client_status", x => x.SetStatus(), "0,15,30,45 * * * * *");
    RecurringJob.AddOrUpdate<CacheJob>("cache", x => x.PopulateCache(), "*/15 * * * *");
    RecurringJob.AddOrUpdate<MarketJob>("market", x => x.Execute(), "*/10 * * * *");

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