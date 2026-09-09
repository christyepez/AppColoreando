using System.Text;
using System.Text.Json;
using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
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

public sealed class FileSystemGenerationArtifactReader : IGenerationArtifactReader
{
    private static readonly IReadOnlyDictionary<string, string> ArtifactProperties =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["thumbnail"] = "thumbnailPath",
            ["catalog"] = "catalogPreviewPath",
            ["lineart"] = "lineArtWebpPath",
            ["special"] = "specialPreviewPath"
        };

    public async Task<GenerationArtifactDto?> ReadAsync(
        string resultManifestPath,
        string artifactKind,
        CancellationToken ct)
    {
        if (!ArtifactProperties.TryGetValue(artifactKind, out var property))
            return null;

        var rootManifest = Path.GetFullPath(resultManifestPath);
        if (!File.Exists(rootManifest)) return null;
        var rootDirectory = Path.GetDirectoryName(rootManifest)!;
        var variantManifest = await ResolvePrimaryManifestAsync(rootManifest, ct);
        if (!IsWithin(rootDirectory, variantManifest) || !File.Exists(variantManifest))
            return null;
        await using var stream = File.OpenRead(variantManifest);
        using var manifest = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!manifest.RootElement.TryGetProperty(property, out var pathNode)) return null;

        var artifactPath = Path.GetFullPath(pathNode.GetString() ?? string.Empty);
        if (!IsWithin(rootDirectory, artifactPath) || !File.Exists(artifactPath)) return null;
        var content = await File.ReadAllBytesAsync(artifactPath, ct);
        var extension = Path.GetExtension(artifactPath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".webp" => "image/webp",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
        return new GenerationArtifactDto(content, contentType, Path.GetFileName(artifactPath));
    }

    private static async Task<string> ResolvePrimaryManifestAsync(string rootManifest, CancellationToken ct)
    {
        await using var stream = File.OpenRead(rootManifest);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (document.RootElement.TryGetProperty("type", out var type) && type.GetString() == "variant-pack" &&
            document.RootElement.TryGetProperty("primaryManifestPath", out var primary))
            return Path.GetFullPath(primary.GetString() ?? rootManifest);
        return rootManifest;
    }

    private static bool IsWithin(string rootDirectory, string candidate)
    {
        var root = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return Path.GetFullPath(candidate).StartsWith(root, comparison);
    }
}
