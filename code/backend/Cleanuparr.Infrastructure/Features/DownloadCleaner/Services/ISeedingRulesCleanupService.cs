using Cleanuparr.Domain.Entities;
using Cleanuparr.Infrastructure.Features.DownloadClient;

namespace Cleanuparr.Infrastructure.Features.DownloadCleaner.Services;

/// <summary>
/// Loads and applies per-client seeding rules to clean completed downloads.
/// </summary>
public interface ISeedingRulesCleanupService
{
    /// <summary>
    /// Evaluates the seeding rules against the client's downloads and removes those that match.
    /// </summary>
    Task CleanAsync(ISeedingCleanupCapable downloadService, List<ITorrentItemWrapper> clientDownloads);

    /// <summary>
    /// Whether the client has a seeding rule with abandon enabled, so its incomplete downloads are worth fetching.
    /// </summary>
    Task<bool> HasAbandonRulesAsync(ISeedingCleanupCapable downloadService);

    /// <summary>
    /// Evaluates the seeding rules with abandon enabled against the client's incomplete downloads and abandons those that match.
    /// </summary>
    Task AbandonAsync(ISeedingCleanupCapable downloadService, List<ITorrentItemWrapper> incompleteDownloads);
}
