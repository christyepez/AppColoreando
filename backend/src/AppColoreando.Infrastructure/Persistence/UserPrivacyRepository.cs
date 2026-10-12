using AppColoreando.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AppColoreando.Infrastructure.Persistence;

public sealed class UserPrivacyRepository(AppDbContext db) : IUserPrivacyRepository
{
    public async Task DeletePersonalDataAsync(Guid userId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.RefreshTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.SyncOperations.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserAchievements.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserActivityHistory.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserMetricsDaily.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserArtworkProgress.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserProfiles.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.AuditLogs.Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UserId, (Guid?)null), ct);

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is not null)
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }
}
