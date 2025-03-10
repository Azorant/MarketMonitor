using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Template.Database;

public class DatabaseContextFactory: IDesignTimeDbContextFactory<DatabaseContext> 
{
    DatabaseContext IDesignTimeDbContextFactory<DatabaseContext>.CreateDbContext(string[] args)
    {
        return new DatabaseContext(CreateDbOptions(new DbContextOptionsBuilder<DatabaseContext>()));
    }

    public static DbContextOptions<DatabaseContext> CreateDbOptions(DbContextOptionsBuilder builder)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
            .Build();
        
        var connectionString = Environment.GetEnvironmentVariable("DB") ?? configuration.GetConnectionString("DB")!;
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        builder
            .UseMySql(connectionString, serverVersion, options => options.UseMicrosoftJson())
            .LogTo(Log.Warning, LogLevel.Warning)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();

        return (builder.Options as DbContextOptions<DatabaseContext>)!;
    }
}