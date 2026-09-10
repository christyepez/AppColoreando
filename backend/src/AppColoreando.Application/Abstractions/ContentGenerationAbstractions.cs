using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Abstractions;

public interface ISourceAssetRepository
{
    Task<SourceAsset?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<SourceAssetDto>> ListAsync(int take, CancellationToken ct);
    Task AddAsync(SourceAsset asset, CancellationToken ct);
}

public interface IStylePresetRepository
{
    Task<StylePreset?> GetAsync(Guid id, CancellationToken ct);
    Task<StylePreset?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyCollection<StylePresetDto>> ListAsync(CancellationToken ct);
    void Add(StylePreset preset);
}

public interface IGenerationJobRepository
{
    Task<ArtworkGenerationJob?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<GenerationJobDto>> ListAsync(int take, CancellationToken ct);
    Task AddAsync(ArtworkGenerationJob job, CancellationToken ct);
}

public interface ISourceAssetStorage
{
    Task<string> SaveAsync(Guid assetId, string fileName, Stream content, CancellationToken ct);
}

public interface IGenerationJobQueue
{
    Task EnqueueAsync(Guid jobId, CancellationToken ct);
}

public interface IContentGenerationService
{
    Task<SourceAssetDto> UploadSourceAssetAsync(
        Guid userId, string fileName, string contentType,
        long sizeBytes, Stream content, CancellationToken ct);
    Task<IReadOnlyCollection<SourceAssetDto>> GetSourceAssetsAsync(int take, CancellationToken ct);
    Task<IReadOnlyCollection<StylePresetDto>> GetStylePresetsAsync(CancellationToken ct);
    Task<GenerationJobDto> CreateGenerationJobAsync(
        Guid userId, CreateGenerationJobRequest request, CancellationToken ct);
    Task<GenerationJobDto?> GetGenerationJobAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<GenerationJobDto>> GetGenerationJobsAsync(int take, CancellationToken ct);
    Task<GenerationArtifactDto?> GetGenerationArtifactAsync(Guid jobId, string artifactKind, CancellationToken ct);
    Task<IReadOnlyCollection<RegionAdjustmentDto>> GetGenerationAdjustmentsAsync(Guid jobId, CancellationToken ct);
    Task<RegionAdjustmentDto> UpsertGenerationAdjustmentAsync(Guid userId, Guid jobId, int regionId, RegionAdjustmentRequest request, CancellationToken ct);
    Task DeleteGenerationAdjustmentAsync(Guid userId, Guid jobId, int regionId, CancellationToken ct);
}

public interface IGenerationArtifactReader
{
    Task<GenerationArtifactDto?> ReadAsync(
        string resultManifestPath,
        string artifactKind,
        CancellationToken ct);
}

public interface IGenerationAdjustmentStore
{
    Task<IReadOnlyCollection<RegionAdjustmentDto>> ListAsync(
        string resultManifestPath, CancellationToken ct);
    Task<RegionAdjustmentDto?> UpsertAsync(
        string resultManifestPath, int regionId, Guid userId,
        RegionAdjustmentRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(
        string resultManifestPath, int regionId, CancellationToken ct);
}

public interface IGenerationPublicationStore
{
    Task<GenerationPublicationAssets?> MaterializeAsync(
        Guid artworkId,
        string resultManifestPath,
        IReadOnlyCollection<RegionAdjustmentDto> adjustments,
        CancellationToken ct);
    Task<GenerationArtifactDto?> ReadPublishedAsync(
        Guid artworkId, string artifactKind, CancellationToken ct);
}

public interface IGenerationPublishingService
{
    Task<ArtworkDto> PublishAsync(
        Guid userId,
        Guid jobId,
        PublishGenerationRequest request,
        CancellationToken ct);
}
