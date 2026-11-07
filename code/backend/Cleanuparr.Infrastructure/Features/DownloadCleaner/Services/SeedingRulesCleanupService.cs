using Cleanuparr.Domain.Entities;
using Cleanuparr.Domain.Enums;
using Cleanuparr.Infrastructure.Features.DownloadClient;
using Cleanuparr.Persistence;
using Cleanuparr.Persistence.Models.Configuration;
using Cleanuparr.Persistence.Models.Configuration.DownloadCleaner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cleanuparr.Infrastructure.Features.DownloadCleaner.Services;

/// <inheritdoc cref="ISeedingRulesCleanupService" />
public sealed class SeedingRulesCleanupService : ISeedingRulesCleanupService
{
    private readonly ILogger<SeedingRulesCleanupService> _logger;
    private readonly DataContext _dataContext;

    public SeedingRulesCleanupService(ILogger<SeedingRulesCleanupService> logger, DataContext dataContext)
    {
        _logger = logger;
        _dataContext = dataContext;
    }

    public async Task CleanAsync(ISeedingCleanupCapable downloadService, List<ITorrentItemWrapper> clientDownloads)
    {
        try
        {
            List<ISeedingRule> seedingRules = await LoadRulesAsync(downloadService.ClientConfig);

            if (seedingRules.Count is 0)
            {
                _logger.LogDebug("No seeding rules found for {clientName}", downloadService.ClientConfig.Name);
                return;
            }
            
            List<ITorrentItemWrapper>? downloadsToClean = downloadService
                .FilterDownloadsToBeCleanedAsync(clientDownloads, seedingRules);

            if (downloadsToClean?.Count is null or 0)
            {
                return;
            }

            _logger.LogInformation("Evaluating {count} downloads for cleanup", downloadsToClean.Count);

            await downloadService.CleanDownloadsAsync(downloadsToClean, seedingRules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean downloads for {clientName}", downloadService.ClientConfig.Name);
        }

        _logger.LogInformation("Finished cleanup evaluation");
    }

    /// <inheritdoc/>
    public async Task<bool> HasAbandonRulesAsync(ISeedingCleanupCapable downloadService) =>
        (await LoadRulesAsync(downloadService.ClientConfig)).Any(rule => rule is IAbandonable { IsAbandonEnabled: true });

    /// <inheritdoc/>
    public async Task AbandonAsync(ISeedingCleanupCapable downloadService, List<ITorrentItemWrapper> incompleteDownloads)
    {
        if (incompleteDownloads.Count is 0)
        {
            return;
        }

        try
        {
            List<ISeedingRule> seedingRules = await LoadRulesAsync(downloadService.ClientConfig);

            List<ITorrentItemWrapper>? downloadsToAbandon = downloadService
                .FilterDownloadsToBeCleanedAsync(incompleteDownloads, seedingRules);

            if (downloadsToAbandon?.Count is null or 0)
            {
                return;
            }

            _logger.LogInformation("Evaluating {count} incomplete downloads for abandoning", downloadsToAbandon.Count);

            await downloadService.AbandonDownloadsAsync(downloadsToAbandon, seedingRules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to abandon downloads for {clientName}", downloadService.ClientConfig.Name);
        }
    }

    private async Task<List<ISeedingRule>> LoadRulesAsync(DownloadClientConfig config) =>
        config.TypeName switch
        {
            DownloadClientTypeName.qBittorrent => (await _dataContext.QBitSeedingRules
                .Where(r => r.DownloadClientConfigId == config.Id).AsNoTracking().ToListAsync()).Cast<ISeedingRule>().ToList(),
            DownloadClientTypeName.Deluge => (await _dataContext.DelugeSeedingRules
                .Where(r => r.DownloadClientConfigId == config.Id).AsNoTracking().ToListAsync()).Cast<ISeedingRule>().ToList(),
            DownloadClientTypeName.Transmission => (await _dataContext.TransmissionSeedingRules
                .Where(r => r.DownloadClientConfigId == config.Id).AsNoTracking().ToListAsync()).Cast<ISeedingRule>().ToList(),
            DownloadClientTypeName.uTorrent => (await _dataContext.UTorrentSeedingRules
                .Where(r => r.DownloadClientConfigId == config.Id).AsNoTracking().ToListAsync()).Cast<ISeedingRule>().ToList(),
            DownloadClientTypeName.rTorrent => (await _dataContext.RTorrentSeedingRules
                .Where(r => r.DownloadClientConfigId == config.Id).AsNoTracking().ToListAsync()).Cast<ISeedingRule>().ToList(),
            _ => []
        };
}
