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
}
