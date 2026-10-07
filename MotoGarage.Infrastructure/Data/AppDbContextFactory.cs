using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MotoGarage.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Migration generation builds metadata without starting the API or opening a connection.
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MotoGarage.API", "appsettings.json")))
            directory = directory.Parent;
        if (directory is null)
            throw new InvalidOperationException("Migration üçün MotoGarage solution qovluğundan işləyin.");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(directory.FullName, "MotoGarage.API", "appsettings.json"))
            .AddJsonFile(Path.Combine(directory.FullName, "MotoGarage.API", $"appsettings.{environment}.json"), optional: true);
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            configurationBuilder.AddUserSecrets("30669b9b-ad4d-42ad-81a6-5aede9f1470d");
        var configuration = configurationBuilder.AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection tələb olunur.");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection).Options;
        return new AppDbContext(options);
    }
}
