using Microsoft.EntityFrameworkCore;
using QuanLyTaiKhoanAPI.Data;

namespace QuanLyTaiKhoanAPI.Services;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);

        var intervalMinutes = Math.Max(5, _configuration.GetValue("RefreshTokenCleanup:IntervalMinutes", 60));
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var retentionDays = Math.Max(1, _configuration.GetValue("RefreshTokenCleanup:RetentionDays", 30));
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var deletedCount = await context.RefreshTokens
                .Where(token => token.Expires < cutoff ||
                    (token.IsRevoked && token.RevokedAt != null && token.RevokedAt < cutoff))
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {Count} old refresh tokens.", deletedCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean up old refresh tokens.");
        }
    }
}
