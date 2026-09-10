namespace AppColoreando.Domain.Entities;

public enum SourceAssetStatus
{
    Uploaded, Validated, Rejected, Processing, Processed
}

public enum GenerationJobStatus
{
    Pending, Queued, Running, PreviewReady, NeedsReview, Approved, Published, Failed
}

public enum GenerationDifficulty
{
    Kids, Easy, Normal, Detailed, Master
}

public sealed class SourceAsset : AuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public SourceAssetStatus Status { get; set; } = SourceAssetStatus.Uploaded;
}

public sealed class StylePreset : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int TargetRegionCount { get; set; }
    public int MinRegionArea { get; set; }
    public int MaxColors { get; set; }
    public double SimplificationTolerance { get; set; }
    public double EdgeSensitivity { get; set; }
    public double CurveSmoothness { get; set; }
    public double SaturationBoost { get; set; }
    public double ContrastBoost { get; set; }
    public bool SemanticMergeEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class ArtworkGenerationJob : AuditableEntity
{
    public Guid SourceAssetId { get; set; }
    public Guid StylePresetId { get; set; }
    public GenerationDifficulty Difficulty { get; set; } = GenerationDifficulty.Normal;
    public GenerationJobStatus Status { get; set; } = GenerationJobStatus.Pending;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ProcessorJobId { get; set; }
    public string? ResultManifestPath { get; set; }
}

public sealed class ArtworkGenerationIssue : AuditableEntity
{
    public Guid ArtworkGenerationJobId { get; set; }
    public string Severity { get; set; } = "Warning";
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}
