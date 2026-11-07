using Cleanuparr.Domain.Entities;
using Cleanuparr.Domain.Enums;
using Cleanuparr.Infrastructure.Features.DownloadCleaner.Services;
using Cleanuparr.Infrastructure.Features.DownloadClient;
using Cleanuparr.Infrastructure.Tests.Features.Jobs.TestHelpers;
using Cleanuparr.Persistence;
using Cleanuparr.Persistence.Models.Configuration;
using Cleanuparr.Persistence.Models.Configuration.DownloadCleaner;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Cleanuparr.Infrastructure.Tests.Features.Jobs;

public sealed class SeedingRulesCleanupServiceAbandonTests : IDisposable
{
    private readonly DataContext _dataContext;
    private readonly ISeedingCleanupCapable _downloadService;
    private readonly DownloadClientConfig _clientConfig;
    private readonly SeedingRulesCleanupService _sut;

    public SeedingRulesCleanupServiceAbandonTests()
    {
        _dataContext = TestDataContextFactory.Create(seedData: false);
        _downloadService = Substitute.For<ISeedingCleanupCapable>();

        _clientConfig = new DownloadClientConfig
        {
            Id = Guid.NewGuid(),
            Name = "Test qBittorrent",
            TypeName = DownloadClientTypeName.qBittorrent,
            Type = DownloadClientType.Torrent,
            Enabled = true,
            Host = new Uri("http://localhost:8080"),
        };
        _downloadService.ClientConfig.Returns(_clientConfig);

        _dataContext.DownloadClients.Add(_clientConfig);
        _dataContext.SaveChanges();

        _sut = new SeedingRulesCleanupService(Substitute.For<ILogger<SeedingRulesCleanupService>>(), _dataContext);
    }

    public void Dispose()
    {
        _dataContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private void AddRule(double abandonBelowPercentage)
    {
        _dataContext.QBitSeedingRules.Add(new QBitSeedingRule
        {
            DownloadClientConfigId = _clientConfig.Id,
            Name = "TorrentLeech",
            Categories = ["tl"],
            MaxRatio = 1,
            AbandonBelowPercentage = abandonBelowPercentage,
            AbandonAfterHours = 24,
        });
        _dataContext.SaveChanges();
    }

    private static ITorrentItemWrapper Torrent(string hash)
    {
        var torrent = Substitute.For<ITorrentItemWrapper>();
        torrent.DownloadId.Returns(hash);
        torrent.Category.Returns("tl");
        return torrent;
    }

    [Fact]
    public async Task HasAbandonRulesAsync_WhenNoRules_ReturnsFalse()
    {
        // Act
        bool result = await _sut.HasAbandonRulesAsync(_downloadService);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasAbandonRulesAsync_WhenOnlyRulesWithAbandonDisabled_ReturnsFalse()
    {
        // Arrange
        AddRule(abandonBelowPercentage: -1);

        // Act
        bool result = await _sut.HasAbandonRulesAsync(_downloadService);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasAbandonRulesAsync_WhenAnAbandonRuleExists_ReturnsTrue()
    {
        // Arrange
        AddRule(abandonBelowPercentage: 10);

        // Act
        bool result = await _sut.HasAbandonRulesAsync(_downloadService);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AbandonAsync_WhenNoIncompleteDownloads_DoesNothing()
    {
        // Arrange
        AddRule(abandonBelowPercentage: 10);

        // Act
        await _sut.AbandonAsync(_downloadService, []);

        // Assert
        await _downloadService.DidNotReceive()
            .AbandonDownloadsAsync(Arg.Any<List<ITorrentItemWrapper>>(), Arg.Any<List<ISeedingRule>>());
    }

    [Fact]
    public async Task AbandonAsync_WhenNoDownloadMatchesARuleCategory_DoesNotAbandon()
    {
        // Arrange
        AddRule(abandonBelowPercentage: 10);
        _downloadService
            .FilterDownloadsToBeCleanedAsync(Arg.Any<List<ITorrentItemWrapper>>(), Arg.Any<List<ISeedingRule>>())
            .Returns([]);

        // Act
        await _sut.AbandonAsync(_downloadService, [Torrent("hash1")]);

        // Assert
        await _downloadService.DidNotReceive()
            .AbandonDownloadsAsync(Arg.Any<List<ITorrentItemWrapper>>(), Arg.Any<List<ISeedingRule>>());
    }

    [Fact]
    public async Task AbandonAsync_PassesMatchingDownloadsAndTheClientRules()
    {
        // Arrange
        AddRule(abandonBelowPercentage: 10);
        ITorrentItemWrapper torrent = Torrent("hash1");
        _downloadService
            .FilterDownloadsToBeCleanedAsync(Arg.Any<List<ITorrentItemWrapper>>(), Arg.Any<List<ISeedingRule>>())
            .Returns(callInfo => callInfo.Arg<List<ITorrentItemWrapper>>());

        // Act
        await _sut.AbandonAsync(_downloadService, [torrent]);

        // Assert
        await _downloadService.Received(1).AbandonDownloadsAsync(
            Arg.Is<List<ITorrentItemWrapper>>(d => d.Single().DownloadId == "hash1"),
            Arg.Is<List<ISeedingRule>>(r => r.Single().Name == "TorrentLeech"));
    }

    [Fact]
    public async Task AbandonAsync_WhenAbandoningThrows_SwallowsTheError()
    {
        // Arrange
        AddRule(abandonBelowPercentage: 10);
        _downloadService
            .FilterDownloadsToBeCleanedAsync(Arg.Any<List<ITorrentItemWrapper>>(), Arg.Any<List<ISeedingRule>>())
            .Returns(callInfo => callInfo.Arg<List<ITorrentItemWrapper>>());
        _downloadService
            .AbandonDownloadsAsync(Arg.Any<List<ITorrentItemWrapper>>(), Arg.Any<List<ISeedingRule>>())
            .ThrowsAsync(new Exception("client hiccup"));

        // Act & Assert
        await Should.NotThrowAsync(() => _sut.AbandonAsync(_downloadService, [Torrent("hash1")]));
    }
}
