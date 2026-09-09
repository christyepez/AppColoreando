namespace AppColoreando.Application.Contracts;

public sealed record VisualProcessorRequest(
    Guid JobId,
    string SourcePath,
    string PresetCode,
    string Difficulty,
    int TargetRegions,
    int MaxColors,
    double SimplificationTolerance,
    double EdgeSensitivity,
    double CurveSmoothness,
    double SaturationBoost,
    double ContrastBoost,
    bool GenerateVariants = true);

public sealed record VisualProcessorResult(
    bool Success,
    string? ProcessorJobId,
    string? ResultManifestPath,
    string? ErrorCode,
    string? ErrorMessage);
