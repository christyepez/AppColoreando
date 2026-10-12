using AppColoreando.Application.Abstractions;
using AppColoreando.Application.Contracts;
using AppColoreando.Domain.Entities;

namespace AppColoreando.Application.Services;

public sealed class AccountPrivacyService(
    IUserRepository users,
    IUserAccountService accounts,
    IUserContentService content,
    IUserActivityRepository activities,
    IUserMetricRepository metrics,
    IUserPrivacyRepository privacy) : IAccountPrivacyService
{
    public async Task<AccountDataExportDto> ExportAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        var profile = await accounts.GetProfileAsync(userId, ct);
        var progress = await content.GetProgressAsync(userId, ct);
        var library = await content.GetLibraryAsync(userId, ct);
        var activity = await activities.ListAsync(userId, int.MaxValue, ct);
        var metricRows = await metrics.ListAsync(userId, int.MaxValue, ct);
        var metricDtos = metricRows.Select(x => new DailyMetricDto(
            x.MetricDate, x.Sessions, x.ArtworksOpened, x.ArtworksCompleted, x.RegionsColored, x.ActiveSeconds)).ToArray();
        var metricsExport = new UserMetricsResponse(
            metricDtos,
            metricRows.Sum(x => x.Sessions),
            metricRows.Sum(x => x.ArtworksOpened),
            metricRows.Sum(x => x.ArtworksCompleted),
            metricRows.Sum(x => x.RegionsColored),
            metricRows.Sum(x => x.ActiveSeconds));
        var achievements = await content.GetAchievementsAsync(userId, ct);
        var sessions = await accounts.GetSessionsAsync(userId, ct);
        var entitlements = await content.GetEntitlementsAsync(userId, ct);

        return new AccountDataExportDto(
            DateTime.UtcNow,
            AuthService.ToUser(user),
            profile.Profile,
            progress,
            library,
            activity,
            metricsExport,
            achievements,
            sessions,
            entitlements);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!string.Equals(user.Role, AppRole.User.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Privileged accounts cannot self-delete.");

        await privacy.DeletePersonalDataAsync(userId, ct);
    }
}
