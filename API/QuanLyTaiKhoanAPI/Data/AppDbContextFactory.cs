using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuanLyTaiKhoanAPI.Data;

// Schema generation does not require a live database or application secrets.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(string.IsNullOrWhiteSpace(connectionString)
                ? "Host=localhost;Database=postgres;Username=postgres" : connectionString,
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.Schema))
            .Options;
        return new AppDbContext(options);
    }
}
