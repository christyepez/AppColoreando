using System.Text.Json;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;
using RabbitMQ.Client;

namespace AppColoreando.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:User is required."),
            Password = configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is required.")
        };
        var queueName = configuration["RabbitMQ:GenerationQueue"] ?? "artwork.generation.requested";

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(queueName, true, false, false, null, cancellationToken: stoppingToken);

        logger.LogInformation("Artwork generation worker listening on {Queue}", queueName);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delivery = await channel.BasicGetAsync(queueName, autoAck: false, cancellationToken: stoppingToken);
            if (delivery is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            try
            {
                var payload = JsonSerializer.Deserialize<QueueMessage>(delivery.Body.Span);
                if (payload is null) throw new InvalidOperationException("Invalid generation queue message.");
                await ProcessJobAsync(payload.JobId, stoppingToken);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generation job message failed.");
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IGenerationJobRepository>();
        var assets = scope.ServiceProvider.GetRequiredService<ISourceAssetRepository>();
        var presets = scope.ServiceProvider.GetRequiredService<IStylePresetRepository>();
        var processor = scope.ServiceProvider.GetRequiredService<IVisualProcessorClient>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = await jobs.GetAsync(jobId, ct) ?? throw new KeyNotFoundException($"Generation job {jobId} not found.");
        var asset = await assets.GetAsync(job.SourceAssetId, ct) ?? throw new KeyNotFoundException("Source asset not found.");
        var preset = await presets.GetAsync(job.StylePresetId, ct) ?? throw new KeyNotFoundException("Style preset not found.");

        job.Status = GenerationJobStatus.Running;
        job.StartedAtUtc = DateTime.UtcNow;
        await uow.SaveChangesAsync(ct);

        var result = await processor.ProcessAsync(new VisualProcessorRequest(
            job.Id, asset.StoragePath, preset.Code, job.Difficulty.ToString(),
            preset.TargetRegionCount, preset.MaxColors, preset.SimplificationTolerance,
            preset.EdgeSensitivity, preset.CurveSmoothness, preset.SaturationBoost,
            preset.ContrastBoost), ct);

        job.CompletedAtUtc = DateTime.UtcNow;
        job.ProcessorJobId = result.ProcessorJobId;
        job.ResultManifestPath = result.ResultManifestPath;
        job.ErrorCode = result.ErrorCode;
        job.ErrorMessage = result.ErrorMessage;
        job.Status = result.Success ? GenerationJobStatus.PreviewReady : GenerationJobStatus.Failed;
        await uow.SaveChangesAsync(ct);
    }

    private sealed record QueueMessage(Guid JobId, DateTime OccurredAtUtc);
}
