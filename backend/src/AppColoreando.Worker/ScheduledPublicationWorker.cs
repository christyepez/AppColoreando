using AppColoreando.Application.Abstractions;

namespace AppColoreando.Worker;

public sealed class ScheduledPublicationWorker(
    ILogger<ScheduledPublicationWorker> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Clamp(
            configuration.GetValue("ScheduledPublishing:IntervalSeconds", 30),
            5,
            3600);

        await PromoteDueAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await PromoteDueAsync(stoppingToken);
    }

    private async Task PromoteDueAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IScheduledPublicationService>();
            var promoted = await service.PromoteDueAsync(DateTime.UtcNow, ct);
            if (promoted > 0)
                logger.LogInformation("Published {Count} scheduled artworks.", promoted);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled artwork publication cycle failed.");
        }
    }
}
