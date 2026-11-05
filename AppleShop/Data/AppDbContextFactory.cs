namespace AppleShop.Data;

// Файл: Data/AppDbContextFactory.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public sealed class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var projectDirectory = Directory.GetCurrentDirectory();

        var settingsPath = Path.Combine(
            projectDirectory,
            "appsettings.json");

        if (!File.Exists(settingsPath))
        {
            throw new InvalidOperationException(
                $"Не знайдено appsettings.json у папці " +
                $"'{projectDirectory}'. Виконуйте dotnet ef " +
                "з папки backend, де розташований appsettings.json.");
        }

        var environment =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable(
                "ASPNETCORE_ENVIRONMENT");

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false);

        if (!string.IsNullOrWhiteSpace(environment))
        {
            configurationBuilder.AddJsonFile(
                $"appsettings.{environment}.json",
                optional: true,
                reloadOnChange: false);
        }

        var configuration = configurationBuilder
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Не задано ConnectionStrings:Default " +
                "у appsettings.json або змінних середовища.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}