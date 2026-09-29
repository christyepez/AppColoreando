using AppColoreando.Application.Abstractions;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Services;

public sealed class ScheduledPublicationService(
    IArtworkRepository artworks,
    IUnitOfWork uow) : IScheduledPublicationService
{
    public async Task<int> PromoteDueAsync(DateTime utcNow, CancellationToken ct)
    {
        var effectiveUtc = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime();
        var due = await artworks.ListDueScheduledAsync(effectiveUtc, 100, ct);
        if (due.Count == 0) return 0;

        foreach (var artwork in due)
        {
            artwork.PublishingStatus = PublishingStatus.Published;
            artwork.PublishedAtUtc = effectiveUtc;
        }

        await uow.SaveChangesAsync(ct);
        return due.Count;
    }
}
