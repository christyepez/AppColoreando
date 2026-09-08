using AppColoreando.Application.Abstractions;

namespace AppColoreando.Worker;

public sealed class Worker(ILogger<Worker> logger, IArtworkBundleProcessor processor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var sample = processor.Validate("""{"palette":[{"id":1,"hex":"#2d7dd2"}],"regions":[{"id":1,"colorId":1,"polygon":[[0,0],[1,0],[1,1]]}]}""");
                logger.LogInformation("Content worker heartbeat at {Time}. Bundle validation available: {IsValid}", DateTimeOffset.UtcNow, sample.IsValid);
            }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
