using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticJobSearch.Infrastructure.Accounts;

public sealed class RecoveryEmailWorker(IServiceScopeFactory scopes, IOptions<PasswordRecoveryOptions> options,
    ILogger<RecoveryEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        var cleanupAt = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<RecoveryEmailProcessor>();
                if (DateTimeOffset.UtcNow >= cleanupAt)
                {
                    await processor.CleanupAsync(stoppingToken);
                    cleanupAt = DateTimeOffset.UtcNow.AddHours(1);
                }
                // Pace the single-instance private beta below Resend's default request rate.
                await processor.ProcessNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception)
            {
                logger.LogError("Password recovery worker failed; will retry. Sensitive details suppressed.");
            }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
