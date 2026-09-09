using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppColoreando.Infrastructure.Persistence;

public sealed class SourceAssetRepository(AppDbContext db) : ISourceAssetRepository
{
    public Task<SourceAsset?> GetAsync(Guid id, CancellationToken ct) =>
        db.SourceAssets.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyCollection<SourceAssetDto>> ListAsync(int take, CancellationToken ct) =>
        await db.SourceAssets.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(take)
            .Select(x => new SourceAssetDto(x.Id, x.FileName, x.ContentType, x.StoragePath,
                x.SizeBytes, x.Width, x.Height, x.Sha256, x.Status, x.CreatedAtUtc))
            .ToArrayAsync(ct);

    public async Task AddAsync(SourceAsset asset, CancellationToken ct) =>
        await db.SourceAssets.AddAsync(asset, ct);
}

public sealed class StylePresetRepository(AppDbContext db) : IStylePresetRepository
{
    public Task<StylePreset?> GetAsync(Guid id, CancellationToken ct) =>
        db.StylePresets.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<StylePreset?> GetByCodeAsync(string code, CancellationToken ct) =>
        db.StylePresets.SingleOrDefaultAsync(x => x.Code == code, ct);

    public async Task<IReadOnlyCollection<StylePresetDto>> ListAsync(CancellationToken ct) =>
        await db.StylePresets.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new StylePresetDto(x.Id, x.Name, x.Code, x.TargetRegionCount,
                x.MinRegionArea, x.MaxColors, x.SimplificationTolerance, x.EdgeSensitivity,
                x.CurveSmoothness, x.SaturationBoost, x.ContrastBoost,
                x.SemanticMergeEnabled, x.IsActive)).ToArrayAsync(ct);

    public void Add(StylePreset preset) => db.StylePresets.Add(preset);
}

public sealed class GenerationJobRepository(AppDbContext db) : IGenerationJobRepository
{
    public Task<ArtworkGenerationJob?> GetAsync(Guid id, CancellationToken ct) =>
        db.ArtworkGenerationJobs.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyCollection<GenerationJobDto>> ListAsync(int take, CancellationToken ct) =>
        await db.ArtworkGenerationJobs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(take)
            .Select(x => new GenerationJobDto(x.Id, x.SourceAssetId, x.StylePresetId,
                x.Difficulty, x.Status, x.ErrorCode, x.ErrorMessage, x.ProcessorJobId,
                x.ResultManifestPath, x.CreatedAtUtc, x.StartedAtUtc, x.CompletedAtUtc))
            .ToArrayAsync(ct);

    public async Task AddAsync(ArtworkGenerationJob job, CancellationToken ct) =>
        await db.ArtworkGenerationJobs.AddAsync(job, ct);
}
