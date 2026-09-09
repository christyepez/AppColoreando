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
            ["special"] = "specialPreviewPath",
            ["regions"] = "regionsPath",
            ["palette"] = "palettePath"
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
            ".json" => "application/json",
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

public sealed class FileSystemGenerationAdjustmentStore : IGenerationAdjustmentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<IReadOnlyCollection<RegionAdjustmentDto>> ListAsync(
        string resultManifestPath, CancellationToken ct)
    {
        var overlayPath = GetOverlayPath(resultManifestPath);
        if (!File.Exists(overlayPath)) return [];
        await using var stream = File.OpenRead(overlayPath);
        return await JsonSerializer.DeserializeAsync<List<RegionAdjustmentDto>>(
            stream, JsonOptions, ct) ?? [];
    }

    public async Task<RegionAdjustmentDto?> UpsertAsync(
        string resultManifestPath, int regionId, Guid userId,
        RegionAdjustmentRequest request, CancellationToken ct)
    {
        if (regionId <= 0 || !await RegionExistsAsync(resultManifestPath, regionId, ct))
            return null;
        ValidateAdjustment(request);
        var items = (await ListAsync(resultManifestPath, ct)).ToList();
        var updated = new RegionAdjustmentDto(
            regionId, NormalizeHex(request.ColorHex),
            TrimOrNull(request.SemanticTag, 64), TrimOrNull(request.SemanticRole, 64),
            request.LabelVisibleAtBase, TrimOrNull(request.Note, 500),
            userId, DateTime.UtcNow);
        var index = items.FindIndex(x => x.RegionId == regionId);
        if (index >= 0) items[index] = updated; else items.Add(updated);
        await SaveAsync(resultManifestPath, items, ct);
        return updated;
    }

    public async Task<bool> DeleteAsync(
        string resultManifestPath, int regionId, CancellationToken ct)
    {
        var items = (await ListAsync(resultManifestPath, ct)).ToList();
        var removed = items.RemoveAll(x => x.RegionId == regionId) > 0;
        if (removed) await SaveAsync(resultManifestPath, items, ct);
        return removed;
    }
    private static async Task<bool> RegionExistsAsync(
        string resultManifestPath, int regionId, CancellationToken ct)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(resultManifestPath))!;
        var primaryManifest = await ResolvePrimaryManifestAsync(resultManifestPath, ct);
        if (!IsWithin(root, primaryManifest) || !File.Exists(primaryManifest)) return false;
        await using var stream = File.OpenRead(primaryManifest);
        using var manifest = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!manifest.RootElement.TryGetProperty("regionsPath", out var regionsNode)) return false;
        var regionsPath = Path.GetFullPath(regionsNode.GetString() ?? string.Empty);
        if (!IsWithin(root, regionsPath) || !File.Exists(regionsPath)) return false;
        await using var regionsStream = File.OpenRead(regionsPath);
        using var regions = await JsonDocument.ParseAsync(regionsStream, cancellationToken: ct);
        return regions.RootElement.EnumerateArray().Any(x =>
            x.TryGetProperty("id", out var id) && id.GetInt32() == regionId);
    }

    private static async Task SaveAsync(
        string resultManifestPath, List<RegionAdjustmentDto> items, CancellationToken ct)
    {
        var path = GetOverlayPath(resultManifestPath);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, items.OrderBy(x => x.RegionId).ToList(), JsonOptions, ct);
    }
    private static string GetOverlayPath(string resultManifestPath)
    {
        var manifest = Path.GetFullPath(resultManifestPath);
        if (!File.Exists(manifest)) throw new FileNotFoundException("Generation manifest not found.", manifest);
        return Path.Combine(Path.GetDirectoryName(manifest)!, "adjustments.json");
    }

    private static async Task<string> ResolvePrimaryManifestAsync(
        string resultManifestPath, CancellationToken ct)
    {
        var rootManifest = Path.GetFullPath(resultManifestPath);
        await using var stream = File.OpenRead(rootManifest);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (document.RootElement.TryGetProperty("type", out var type) &&
            type.GetString() == "variant-pack" &&
            document.RootElement.TryGetProperty("primaryManifestPath", out var primary))
            return Path.GetFullPath(primary.GetString() ?? rootManifest);
        return rootManifest;
    }
    private static void ValidateAdjustment(RegionAdjustmentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ColorHex) &&
            !System.Text.RegularExpressions.Regex.IsMatch(request.ColorHex, "^#[0-9A-Fa-f]{6}$"))
            throw new ArgumentException("ColorHex must use #RRGGBB format.");
    }

    private static string? NormalizeHex(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"Value exceeds {maxLength} characters.");
        return trimmed;
    }

    private static bool IsWithin(string rootDirectory, string candidate)
    {
        var root = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return Path.GetFullPath(candidate).StartsWith(root, comparison);
    }
}
