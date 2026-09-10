using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Contracts;

public sealed record SourceAssetDto(
    Guid Id, string FileName, string ContentType, string StoragePath,
    long SizeBytes, int? Width, int? Height, string Sha256,
    SourceAssetStatus Status, DateTime CreatedAtUtc);

public sealed record StylePresetDto(
    Guid Id, string Name, string Code, int TargetRegionCount,
    int MinRegionArea, int MaxColors, double SimplificationTolerance,
    double EdgeSensitivity, double CurveSmoothness, double SaturationBoost,
    double ContrastBoost, bool SemanticMergeEnabled, bool IsActive);

public sealed record CreateGenerationJobRequest(
    Guid SourceAssetId,
    Guid StylePresetId,
    GenerationDifficulty Difficulty = GenerationDifficulty.Normal);

public sealed record GenerationJobDto(
    Guid Id, Guid SourceAssetId, Guid StylePresetId,
    GenerationDifficulty Difficulty, GenerationJobStatus Status,
    string? ErrorCode, string? ErrorMessage, string? ProcessorJobId,
    string? ResultManifestPath, DateTime CreatedAtUtc,
    DateTime? StartedAtUtc, DateTime? CompletedAtUtc);

public sealed record GenerationArtifactDto(
    byte[] Content,
    string ContentType,
    string FileName);

public sealed record RegionAdjustmentRequest(
    string? ColorHex,
    string? SemanticTag,
    string? SemanticRole,
    bool? LabelVisibleAtBase,
    string? Note);

public sealed record RegionAdjustmentDto(
    int RegionId,
    string? ColorHex,
    string? SemanticTag,
    string? SemanticRole,
    bool? LabelVisibleAtBase,
    string? Note,
    Guid UpdatedByUserId,
    DateTime UpdatedAtUtc);

public sealed record PublishGenerationRequest(
    string Title,
    Guid CategoryId,
    string? CountryCode,
    string? Description = null);

public sealed record GenerationPublicationAssets(
    string BundlePath,
    string ThumbnailPath,
    int RegionCount,
    string Checksum);

public sealed record GenerationQueueMessage(Guid JobId, DateTime OccurredAtUtc);
