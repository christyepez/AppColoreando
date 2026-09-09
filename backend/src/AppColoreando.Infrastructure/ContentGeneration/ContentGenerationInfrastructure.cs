using System.Text;
using System.Text.Json;
using AppColoreando.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace AppColoreando.Infrastructure.ContentGeneration;

public sealed class FileSystemSourceAssetStorage(IConfiguration configuration) : ISourceAssetStorage
{
    private readonly string root = Path.GetFullPath(
        configuration["ContentGeneration:SourceAssetRoot"]
        ?? Path.Combine(AppContext.BaseDirectory, "content-data", "source-assets"));

    public async Task<string> SaveAsync(
        Guid assetId, string fileName, Stream content, CancellationToken ct)
    {
        var safeName = Path.GetFileName(fileName);
        var directory = Path.Combine(root, assetId.ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, safeName);
        await using var output = File.Create(path);
        await content.CopyToAsync(output, ct);
        return path;
    }
}

public sealed class RabbitMqGenerationJobQueue(IConfiguration configuration) : IGenerationJobQueue
{
    private readonly string host = configuration["RabbitMQ:Host"] ?? "localhost";
    private readonly string user = configuration["RabbitMQ:User"] ?? throw new InvalidOperationException("RabbitMQ:User is required.");
    private readonly string password = configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is required.");
    private readonly string queueName = configuration["RabbitMQ:GenerationQueue"] ?? "artwork.generation.requested";

    public async Task EnqueueAsync(Guid jobId, CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = password
        };
        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(
            queue: queueName, durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: ct);

        var payload = JsonSerializer.Serialize(new { jobId, occurredAtUtc = DateTime.UtcNow });
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };
        await channel.BasicPublishAsync(
            exchange: string.Empty, routingKey: queueName, mandatory: false,
            basicProperties: properties, body: body, cancellationToken: ct);
    }
}
