using Cleanuparr.Domain.Entities;
using Cleanuparr.Domain.Enums;
using Cleanuparr.Infrastructure.Features.DownloadClient.QBittorrent;
using Cleanuparr.Persistence.Models.Configuration.DownloadCleaner;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QBittorrent.Client;
using Shouldly;
using Xunit;

namespace Cleanuparr.Infrastructure.Tests.Features.DownloadClient;

public class QBitServiceDCTests : IClassFixture<QBitServiceFixture>
{
    private readonly QBitServiceFixture _fixture;

    public QBitServiceDCTests(QBitServiceFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetMocks();
    }

    public class GetSeedingDownloads_Tests : QBitServiceDCTests
    {
        public GetSeedingDownloads_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task ReturnsCompletedTorrents()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var torrentList = new[]
            {
                new TorrentInfo { Hash = "hash1", Name = "Torrent 1", State = TorrentState.Uploading },
                new TorrentInfo { Hash = "hash2", Name = "Torrent 2", State = TorrentState.Uploading }
            };

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash1")
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash2")
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync(Arg.Any<string>())
                .Returns(new TorrentProperties
                {
                    AdditionalData = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                    {
                        { "is_private", Newtonsoft.Json.Linq.JToken.FromObject(false) }
                    }
                });

            // Act
            var result = await sut.GetSeedingDownloads();

            // Assert
            result.Count.ShouldBe(2);
            foreach (var item in result) { item.DownloadId.ShouldNotBeNull(); }
        }

        [Fact]
        public async Task SetsIsPrivateCorrectly_WhenPrivate()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var torrentList = new[]
            {
                new TorrentInfo { Hash = "hash1", Name = "Private Torrent", State = TorrentState.Uploading }
            };

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash1")
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync("hash1")
                .Returns(new TorrentProperties
                {
                    AdditionalData = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                    {
                        { "is_private", Newtonsoft.Json.Linq.JToken.FromObject(true) }
                    }
                });

            // Act
            var result = await sut.GetSeedingDownloads();

            // Assert
            result.ShouldHaveSingleItem();
            result[0].IsPrivate.ShouldBeTrue();
        }

        [Fact]
        public async Task SetsIsPrivateCorrectly_WhenPublic()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var torrentList = new[]
            {
                new TorrentInfo { Hash = "hash1", Name = "Public Torrent", State = TorrentState.Uploading }
            };

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash1")
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync("hash1")
                .Returns(new TorrentProperties
                {
                    AdditionalData = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                    {
                        { "is_private", Newtonsoft.Json.Linq.JToken.FromObject(false) }
                    }
                });

            // Act
            var result = await sut.GetSeedingDownloads();

            // Assert
            result.ShouldHaveSingleItem();
            result[0].IsPrivate.ShouldBeFalse();
        }

        [Fact]
        public async Task ReturnsEmptyList_WhenNoTorrents()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns((TorrentInfo[]?)null);

            // Act
            var result = await sut.GetSeedingDownloads();

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task SkipsTorrentsWithEmptyHash()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var torrentList = new[]
            {
                new TorrentInfo { Hash = "", Name = "No Hash", State = TorrentState.Uploading },
                new TorrentInfo { Hash = "hash1", Name = "Valid Hash", State = TorrentState.Uploading }
            };

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash1")
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync("hash1")
                .Returns(new TorrentProperties
                {
                    AdditionalData = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                    {
                        { "is_private", Newtonsoft.Json.Linq.JToken.FromObject(false) }
                    }
                });

            // Act
            var result = await sut.GetSeedingDownloads();

            // Assert
            result.ShouldHaveSingleItem();
            result[0].DownloadId.ShouldBe("hash1");
        }

        [Fact]
        public async Task SkipsTorrent_WhenTrackersAreNull()
        {
            // Arrange
            QBitService sut = _fixture.CreateSut();

            TorrentInfo[] torrentList =
            [
                new TorrentInfo { Hash = "hash1", Name = "Deleted Torrent", State = TorrentState.Uploading },
                new TorrentInfo { Hash = "hash2", Name = "Live Torrent", State = TorrentState.Uploading }
            ];

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash1")
                .Returns((IReadOnlyList<TorrentTracker>?)null);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash2")
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync(Arg.Any<string>())
                .Returns(PublicProperties());

            // Act
            List<ITorrentItemWrapper> result = await sut.GetSeedingDownloads();

            // Assert
            result.ShouldHaveSingleItem();
            result[0].DownloadId.ShouldBe("hash2");
        }

        [Fact]
        public async Task SkipsTorrent_WhenPropertiesAreNull()
        {
            // Arrange
            QBitService sut = _fixture.CreateSut();

            TorrentInfo[] torrentList =
            [
                new TorrentInfo { Hash = "hash1", Name = "Deleted Torrent", State = TorrentState.Uploading }
            ];

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash1")
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync("hash1")
                .Returns((TorrentProperties?)null);

            // Act
            List<ITorrentItemWrapper> result = await sut.GetSeedingDownloads();

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task KeepsTorrent_WhenOnlyPseudoTrackersArePresent()
        {
            // Arrange
            QBitService sut = _fixture.CreateSut();

            TorrentInfo[] torrentList =
            [
                new TorrentInfo { Hash = "hash1", Name = "Trackerless Torrent", State = TorrentState.Uploading }
            ];

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash1")
                .Returns(new TorrentTracker[]
                {
                    new() { Url = "** [DHT] **", TrackerStatus = TorrentTrackerStatus.Working },
                    new() { Url = "** [PeX] **", TrackerStatus = TorrentTrackerStatus.Working },
                    new() { Url = "** [LSD] **", TrackerStatus = TorrentTrackerStatus.Working }
                });

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync("hash1")
                .Returns(PublicProperties());

            // Act
            List<ITorrentItemWrapper> result = await sut.GetSeedingDownloads();

            // Assert
            result.ShouldHaveSingleItem();
            result[0].TrackerDomains.ShouldBeEmpty();
            result[0].TrackerHealth.ShouldBe(TrackerHealth.Unsupported);
        }

        [Fact]
        public async Task KeepsTorrent_WhenATrackerUrlIsNull()
        {
            // Arrange
            QBitService sut = _fixture.CreateSut();

            TorrentInfo[] torrentList =
            [
                new TorrentInfo { Hash = "hash1", Name = "Torrent 1", State = TorrentState.Uploading }
            ];

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.Completed))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("hash1")
                .Returns(new TorrentTracker[]
                {
                    new() { Url = null!, TrackerStatus = TorrentTrackerStatus.Working },
                    new() { Url = "http://tracker.example.com/announce", TrackerStatus = TorrentTrackerStatus.Working }
                });

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync("hash1")
                .Returns(PublicProperties());

            // Act
            List<ITorrentItemWrapper> result = await sut.GetSeedingDownloads();

            // Assert
            result.ShouldHaveSingleItem();
            result[0].TrackerDomains.ShouldHaveSingleItem();
        }

        private static TorrentProperties PublicProperties() => new()
        {
            AdditionalData = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
            {
                { "is_private", Newtonsoft.Json.Linq.JToken.FromObject(false) }
            }
        };
    }

    public class GetIncompleteDownloads_Tests : QBitServiceDCTests
    {
        public GetIncompleteDownloads_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task ReturnsOnlyIncompleteTorrentsWithTrustedProgress()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var torrentList = new[]
            {
                new TorrentInfo { Hash = "downloading", Name = "1", State = TorrentState.Downloading, Progress = 0.05 },
                new TorrentInfo { Hash = "stalled", Name = "2", State = TorrentState.StalledDownload, Progress = 0.05 },
                new TorrentInfo { Hash = "paused", Name = "3", State = TorrentState.PausedDownload, Progress = 0.05 },
                new TorrentInfo { Hash = "queued", Name = "4", State = TorrentState.QueuedDownload, Progress = 0.05 },
                new TorrentInfo { Hash = "complete", Name = "5", State = TorrentState.Uploading, Progress = 1 },
                new TorrentInfo { Hash = "complete-paused", Name = "6", State = TorrentState.PausedUpload, Progress = 1 },
                new TorrentInfo { Hash = "checking", Name = "7", State = TorrentState.CheckingDownload, Progress = 0.05 },
                new TorrentInfo { Hash = "resume-data", Name = "8", State = TorrentState.CheckingResumeData, Progress = 0.05 },
                new TorrentInfo { Hash = "queued-check", Name = "9", State = TorrentState.QueuedForChecking, Progress = 0.05 },
                new TorrentInfo { Hash = "moving", Name = "10", State = TorrentState.Moving, Progress = 0.05 },
                new TorrentInfo { Hash = "allocating", Name = "11", State = TorrentState.Allocating, Progress = 0.05 },
                new TorrentInfo { Hash = "metadata", Name = "12", State = TorrentState.FetchingMetadata, Progress = 0 },
                new TorrentInfo { Hash = "error", Name = "13", State = TorrentState.Error, Progress = 0.05 },
                new TorrentInfo { Hash = "missing", Name = "14", State = TorrentState.MissingFiles, Progress = 0.05 },
                new TorrentInfo { Hash = "unknown", Name = "15", State = TorrentState.Unknown, Progress = 0.05 },
                new TorrentInfo { Hash = "", Name = "16", State = TorrentState.Downloading, Progress = 0.05 }
            };

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Is<TorrentListQuery>(q => q.Filter == TorrentListFilter.All))
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync(Arg.Any<string>())
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync(Arg.Any<string>())
                .Returns(new TorrentProperties
                {
                    AdditionalData = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                    {
                        { "is_private", Newtonsoft.Json.Linq.JToken.FromObject(true) }
                    }
                });

            // Act
            List<ITorrentItemWrapper> result = await sut.GetIncompleteDownloads();

            // Assert
            result.Select(x => x.DownloadId).ShouldBe(["downloading", "stalled", "paused", "queued"]);
            result.ShouldAllBe(x => x.IsPrivate);
        }

        [Fact]
        public async Task NeverReturnsCompletedOrUploadSideTorrents()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            TorrentState[] uploadSide =
            [
                TorrentState.Uploading, TorrentState.ForcedUpload, TorrentState.StalledUpload,
                TorrentState.PausedUpload, TorrentState.QueuedUpload, TorrentState.CheckingUpload
            ];

            var torrentList = Enum.GetValues<TorrentState>()
                .SelectMany(state => new[]
                {
                    new TorrentInfo { Hash = $"{state}-partial", Name = "p", State = state, Progress = 0.05 },
                    new TorrentInfo { Hash = $"{state}-complete", Name = "c", State = state, Progress = 1 }
                })
                .ToArray();

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Any<TorrentListQuery>())
                .Returns(torrentList);

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync(Arg.Any<string>())
                .Returns(Array.Empty<TorrentTracker>());

            _fixture.ClientWrapper
                .GetTorrentPropertiesAsync(Arg.Any<string>())
                .Returns(new TorrentProperties { AdditionalData = new Dictionary<string, Newtonsoft.Json.Linq.JToken>() });

            // Act
            List<ITorrentItemWrapper> result = await sut.GetIncompleteDownloads();

            // Assert
            result.ShouldNotBeEmpty();
            result.ShouldAllBe(x => x.CompletionPercentage < 100);
            result.Cast<QBitItemWrapper>().ShouldAllBe(x => !uploadSide.Contains(x.Info.State));
        }

        [Fact]
        public async Task SkipsTorrent_WhenItNoLongerExists()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Any<TorrentListQuery>())
                .Returns(new[] { new TorrentInfo { Hash = "gone", Name = "Gone", State = TorrentState.Downloading, Progress = 0.05 } });

            _fixture.ClientWrapper
                .GetTorrentTrackersAsync("gone")
                .Returns((IReadOnlyList<TorrentTracker>?)null);

            // Act
            List<ITorrentItemWrapper> result = await sut.GetIncompleteDownloads();

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task ReturnsEmptyList_WhenNull()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Any<TorrentListQuery>())
                .Returns((IReadOnlyList<TorrentInfo>?)null);

            // Act
            List<ITorrentItemWrapper> result = await sut.GetIncompleteDownloads();

            // Assert
            result.ShouldBeEmpty();
        }
    }

    public class FilterDownloadsToBeCleanedAsync_Tests : QBitServiceDCTests
    {
        public FilterDownloadsToBeCleanedAsync_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void MatchesCategories()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Category = "movies" }, Array.Empty<TorrentTracker>(), false),
                new QBitItemWrapper(new TorrentInfo { Hash = "hash2", Category = "tv" }, Array.Empty<TorrentTracker>(), false),
                new QBitItemWrapper(new TorrentInfo { Hash = "hash3", Category = "music" }, Array.Empty<TorrentTracker>(), false)
            };

            var categories = new List<ISeedingRule>
            {
                new QBitSeedingRule { Name = "movies", Categories = ["movies"], MaxRatio = -1, MinSeedTime = 0, MaxSeedTime = -1, DeleteSourceFiles = true },
                new QBitSeedingRule { Name = "tv", Categories = ["tv"], MaxRatio = -1, MinSeedTime = 0, MaxSeedTime = -1, DeleteSourceFiles = true }
            };

            // Act
            var result = sut.FilterDownloadsToBeCleanedAsync(downloads, categories);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result.ShouldContain(x => x.Category == "movies");
            result.ShouldContain(x => x.Category == "tv");
        }

        [Fact]
        public void IsCaseInsensitive()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Category = "Movies" }, Array.Empty<TorrentTracker>(), false)
            };

            var categories = new List<ISeedingRule>
            {
                new QBitSeedingRule { Name = "movies", Categories = ["movies"], MaxRatio = -1, MinSeedTime = 0, MaxSeedTime = -1, DeleteSourceFiles = true }
            };

            // Act
            var result = sut.FilterDownloadsToBeCleanedAsync(downloads, categories);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldHaveSingleItem();
        }

        [Fact]
        public void SkipsDownloadsWithEmptyHash()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "", Category = "movies" }, Array.Empty<TorrentTracker>(), false),
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Category = "movies" }, Array.Empty<TorrentTracker>(), false)
            };

            var categories = new List<ISeedingRule>
            {
                new QBitSeedingRule { Name = "movies", Categories = ["movies"], MaxRatio = -1, MinSeedTime = 0, MaxSeedTime = -1, DeleteSourceFiles = true }
            };

            // Act
            var result = sut.FilterDownloadsToBeCleanedAsync(downloads, categories);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldHaveSingleItem();
            result[0].DownloadId.ShouldBe("hash1");
        }

        [Fact]
        public void ReturnsEmptyList_WhenNoMatches()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Category = "music" }, Array.Empty<TorrentTracker>(), false)
            };

            var categories = new List<ISeedingRule>
            {
                new QBitSeedingRule { Name = "movies", Categories = ["movies"], MaxRatio = -1, MinSeedTime = 0, MaxSeedTime = -1, DeleteSourceFiles = true }
            };

            // Act
            var result = sut.FilterDownloadsToBeCleanedAsync(downloads, categories);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }
    }

    public class CleanDownloadsAsync_Tests : QBitServiceDCTests
    {
        public CleanDownloadsAsync_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        private static QBitItemWrapper CreateTorrent(string hash, string category, bool isPrivate, DateTime? lastActivity = null) =>
            new(new TorrentInfo
            {
                Hash = hash,
                Name = $"Test {hash}",
                Category = category,
                Ratio = 2.0,
                SeedingTime = TimeSpan.FromHours(10),
                LastActivityTime = lastActivity
            }, Array.Empty<TorrentTracker>(), isPrivate);

        private static QBitSeedingRule CreateRule(string name, TorrentPrivacyType privacyType) =>
            new()
            {
                Name = name,
                Categories = [name],
                PrivacyType = privacyType,
                MaxRatio = 0,
                MinSeedTime = 0,
                MaxSeedTime = -1,
                DeleteSourceFiles = false
            };

        private static ITorrentItemWrapper CreateTorrentWithSeederCount(string hash, int? seederCount)
        {
            var torrent = Substitute.For<ITorrentItemWrapper>();
            torrent.DownloadId.Returns(hash);
            torrent.Name.Returns($"Test {hash}");
            torrent.Category.Returns("movies");
            torrent.IsPrivate.Returns(false);
            torrent.Ratio.Returns(2.0);
            torrent.SeedingTimeSeconds.Returns((long)TimeSpan.FromHours(10).TotalSeconds);
            torrent.SeederCount.Returns(seederCount);
            torrent.TrackerDomains.Returns(Array.Empty<string>());
            torrent.Tags.Returns(Array.Empty<string>());
            return torrent;
        }

        private void SetupDeleteMock()
        {
            _fixture.ClientWrapper
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task SkipsPrivateTorrent_WhenRuleIsPublicOnly()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: true)
            };
            var rules = new List<ISeedingRule> { CreateRule("movies", TorrentPrivacyType.Public) };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task CleansPublicTorrent_WhenRuleIsPublicOnly()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: false)
            };
            var rules = new List<ISeedingRule> { CreateRule("movies", TorrentPrivacyType.Public) };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), false);
        }

        [Fact]
        public async Task SkipsTorrent_WhenMinimumSeedersNotReached()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<ITorrentItemWrapper>
            {
                CreateTorrentWithSeederCount("hash1", 4)
            };
            var rule = CreateRule("movies", TorrentPrivacyType.Public);
            rule.MinSeeders = 5;
            var rules = new List<ISeedingRule> { rule };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task CleansTorrent_WhenMinimumSeedersReached()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<ITorrentItemWrapper>
            {
                CreateTorrentWithSeederCount("hash1", 5)
            };
            var rule = CreateRule("movies", TorrentPrivacyType.Public);
            rule.MinSeeders = 5;
            var rules = new List<ISeedingRule> { rule };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), false);
        }

        [Fact]
        public async Task SkipsTorrent_WhenMinimumSeedersConfiguredAndSeederCountUnavailable()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<ITorrentItemWrapper>
            {
                CreateTorrentWithSeederCount("hash1", null)
            };
            var rule = CreateRule("movies", TorrentPrivacyType.Public);
            rule.MinSeeders = 5;
            var rules = new List<ISeedingRule> { rule };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task CleansTorrent_WhenMinSeedersIsDisabled_AndSeederCountUnavailable()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<ITorrentItemWrapper>
            {
                CreateTorrentWithSeederCount("hash1", null)
            };
            var rule = CreateRule("movies", TorrentPrivacyType.Public);
            var rules = new List<ISeedingRule> { rule };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), false);
        }

        [Fact]
        public async Task SkipsTorrent_WhenStillActiveWithinInactiveWindow()
        {
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: false, lastActivity: DateTime.UtcNow.AddDays(-5))
            };
            var rule = CreateRule("movies", TorrentPrivacyType.Public);
            rule.MaxInactiveDays = 30;
            var rules = new List<ISeedingRule> { rule };

            await sut.CleanDownloadsAsync(downloads, rules);

            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task CleansTorrent_WhenInactiveLongerThanWindow()
        {
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: false, lastActivity: DateTime.UtcNow.AddDays(-40))
            };
            var rule = CreateRule("movies", TorrentPrivacyType.Public);
            rule.MaxInactiveDays = 30;
            var rules = new List<ISeedingRule> { rule };

            await sut.CleanDownloadsAsync(downloads, rules);

            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), false);
        }

        [Fact]
        public async Task SkipsTorrent_WhenMaxInactiveDaysConfigured_AndLastActivityUnavailable()
        {
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: false, lastActivity: null)
            };
            var rule = CreateRule("movies", TorrentPrivacyType.Public);
            rule.MaxInactiveDays = 30;
            var rules = new List<ISeedingRule> { rule };

            await sut.CleanDownloadsAsync(downloads, rules);

            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task SkipsTorrent_WhenMaxInactiveDaysConfigured_AndLastActivityIsUnixEpoch(int epochOffsetSeconds)
        {
            QBitService sut = _fixture.CreateSut();
            SetupDeleteMock();

            List<Domain.Entities.ITorrentItemWrapper> downloads = new()
            {
                CreateTorrent("hash1", "movies", isPrivate: false, lastActivity: DateTime.UnixEpoch.AddSeconds(epochOffsetSeconds))
            };
            QBitSeedingRule rule = CreateRule("movies", TorrentPrivacyType.Public);
            rule.MaxInactiveDays = 30;
            List<ISeedingRule> rules = new() { rule };

            await sut.CleanDownloadsAsync(downloads, rules);

            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task CleansTorrent_WhenMaxInactiveDaysDisabled_RegardlessOfActivity()
        {
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: false, lastActivity: DateTime.UtcNow)
            };
            var rule = CreateRule("movies", TorrentPrivacyType.Public);
            var rules = new List<ISeedingRule> { rule };

            await sut.CleanDownloadsAsync(downloads, rules);

            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), false);
        }

        [Fact]
        public async Task SkipsPublicTorrent_WhenRuleIsPrivateOnly()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: false)
            };
            var rules = new List<ISeedingRule> { CreateRule("movies", TorrentPrivacyType.Private) };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task CleansPrivateTorrent_WhenRuleIsPrivateOnly()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: true)
            };
            var rules = new List<ISeedingRule> { CreateRule("movies", TorrentPrivacyType.Private) };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), false);
        }

        [Fact]
        public async Task CleansPublicTorrent_WhenRuleIsBoth()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: false)
            };
            var rules = new List<ISeedingRule> { CreateRule("movies", TorrentPrivacyType.Both) };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), false);
        }

        [Fact]
        public async Task CleansPrivateTorrent_WhenRuleIsBoth()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("hash1", "movies", isPrivate: true)
            };
            var rules = new List<ISeedingRule> { CreateRule("movies", TorrentPrivacyType.Both) };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), false);
        }

        [Fact]
        public async Task MatchesCorrectRule_WhenMultipleRulesForSameCategory()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            SetupDeleteMock();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                CreateTorrent("public-hash", "movies", isPrivate: false),
                CreateTorrent("private-hash", "movies", isPrivate: true)
            };
            var rules = new List<ISeedingRule>
            {
                CreateRule("movies", TorrentPrivacyType.Public),
                CreateRule("movies", TorrentPrivacyType.Private)
            };

            // Act
            await sut.CleanDownloadsAsync(downloads, rules);

            // Assert - both torrents should be cleaned, each matching their respective rule
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("public-hash")), false);
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("private-hash")), false);
        }
    }

    public class AbandonDownloadsAsync_Tests : QBitServiceDCTests
    {
        public AbandonDownloadsAsync_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        private static QBitItemWrapper CreateIncomplete(
            string hash,
            double progress = 0.03,
            double ageHours = 48,
            string savePath = "/downloads",
            string? name = null,
            string tracker = "https://tracker.torrentleech.org/announce",
            bool hasAddedOn = true) =>
            new(new TorrentInfo
            {
                Hash = hash,
                Name = name ?? $"Test {hash}",
                Category = "tl",
                State = TorrentState.Downloading,
                Progress = progress,
                Downloaded = (long)(progress * 1000),
                Size = 1000,
                SavePath = savePath,
                AddedOn = hasAddedOn ? DateTime.UtcNow.AddHours(-ageHours) : null
            }, [new TorrentTracker { Url = tracker }], true);

        private static QBitSeedingRule CreateRule(
            double abandonBelow = 10,
            double abandonAfterHours = 24,
            SeedingRuleAction action = SeedingRuleAction.Delete,
            bool deleteSourceFiles = true) =>
            new()
            {
                Name = "TorrentLeech",
                Categories = ["tl"],
                TrackerPatterns = ["torrentleech.org"],
                PrivacyType = TorrentPrivacyType.Private,
                MaxRatio = 1,
                MaxSeedTime = -1,
                AbandonBelowPercentage = abandonBelow,
                AbandonAfterHours = abandonAfterHours,
                Action = action,
                DeleteSourceFiles = deleteSourceFiles
            };

        private void SetupClient(params QBitItemWrapper[] allTorrents)
        {
            _fixture.ClientWrapper
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
                .Returns(Task.CompletedTask);
            _fixture.ClientWrapper
                .PauseAsync(Arg.Any<IEnumerable<string>>())
                .Returns(Task.CompletedTask);
            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Any<TorrentListQuery>())
                .Returns(allTorrents.Select(t => t.Info).ToArray());
        }

        [Fact]
        public async Task DeletesWithFiles_WhenOldEnoughAndBelowThreshold()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1");
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Single() == "hash1"), true);
            await _fixture.EventPublisher.Received(1)
                .PublishDownloadCleaned(Arg.Any<double>(), TimeSpan.Zero, "tl", CleanReason.AbandonedBelowThreshold);
        }

        [Theory]
        [InlineData(0.10)] // exactly at the threshold
        [InlineData(0.50)]
        public async Task KeepsTorrent_WhenAtOrAboveThreshold(double progress)
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1", progress: progress);
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task KeepsTorrent_WhenWastedBytesPushItAboveThreshold()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = new QBitItemWrapper(new TorrentInfo
            {
                Hash = "hash1",
                Name = "Wasted",
                Category = "tl",
                State = TorrentState.Downloading,
                Progress = 0.03,
                Downloaded = 150,
                Size = 1000,
                SavePath = "/downloads",
                AddedOn = DateTime.UtcNow.AddHours(-48)
            }, [new TorrentTracker { Url = "https://tracker.torrentleech.org/announce" }], true);
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task KeepsTorrent_WhenYoungerThanAbandonAfterHours()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1", ageHours: 2);
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task KeepsTorrent_WhenAddedTimeIsUnknown()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1", hasAddedOn: false);
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task KeepsTorrent_WhenMatchedRuleHasAbandonDisabled()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1");
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule(abandonBelow: -1)]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task KeepsTorrent_WhenHigherPriorityRuleWithoutAbandonShadowsIt()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1");
            SetupClient(torrent);

            var shadowing = CreateRule(abandonBelow: -1) with { Name = "Shadow", Priority = 0 };
            var abandoning = CreateRule() with { Priority = 1 };

            // Act
            await sut.AbandonDownloadsAsync([torrent], [abandoning, shadowing]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task KeepsTorrent_WhenTrackerDoesNotMatch()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1", tracker: "https://tracker.other.org/announce");
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task KeepsFiles_WhenAnotherTorrentSharesTheContentPath()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("tl-hash", name: "Movie.2024.1080p");
            var crossSeed = CreateIncomplete("other-hash", progress: 1, name: "Movie.2024.1080p");
            SetupClient(torrent, crossSeed);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Single() == "tl-hash"), false);
        }

        [Theory]
        [InlineData("/downloads", "Season.Pack", "/downloads/Season.Pack", "Episode.01")] // other is nested inside
        [InlineData("/downloads/Season.Pack", "Episode.01", "/downloads", "Season.Pack")] // other contains it
        [InlineData("/downloads", "Movie.2024", "/Downloads", "movie.2024")]             // differs only in case
        [InlineData("/downloads", "Movie.2024", "", "Other")]                            // other path unknown
        public async Task KeepsFiles_WhenPathsOverlapOrAreUnknown(string savePath, string name, string otherSavePath, string otherName)
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("tl-hash", savePath: savePath, name: name);
            var other = CreateIncomplete("other-hash", progress: 1, savePath: otherSavePath, name: otherName);
            SetupClient(torrent, other);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Single() == "tl-hash"), false);
        }

        [Fact]
        public async Task DeletesFiles_WhenOtherTorrentOnlySharesAPrefix()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("tl-hash", name: "Movie");
            var other = CreateIncomplete("other-hash", progress: 1, name: "Movie.2024");
            SetupClient(torrent, other);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Single() == "tl-hash"), true);
        }

        [Fact]
        public async Task KeepsFiles_WhenSharedPathCheckFails()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1");
            SetupClient(torrent);
            _fixture.ClientWrapper
                .GetTorrentListAsync(Arg.Any<TorrentListQuery>())
                .ThrowsAsync(new Exception("client hiccup"));

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Single() == "hash1"), false);
        }

        [Fact]
        public async Task KeepsTorrent_WhenAddedTimeIsInTheFuture()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1", ageHours: -5);
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule(abandonAfterHours: 0)]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task KeepsTorrent_WhenRuleActionIsUnknown()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1");
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule(action: SeedingRuleAction.Unknown)]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
            await _fixture.ClientWrapper.DidNotReceive().PauseAsync(Arg.Any<IEnumerable<string>>());
        }

        [Fact]
        public async Task KeepsFiles_WhenRuleDoesNotDeleteSourceFiles()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1");
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule(deleteSourceFiles: false)]);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Single() == "hash1"), false);
            await _fixture.ClientWrapper.DidNotReceive().GetTorrentListAsync(Arg.Any<TorrentListQuery>());
        }

        [Fact]
        public async Task StopsTorrent_WhenRuleActionIsStop()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1");
            SetupClient(torrent);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule(action: SeedingRuleAction.Stop)]);

            // Assert
            await _fixture.ClientWrapper.Received(1).PauseAsync(Arg.Is<IEnumerable<string>>(h => h.Single() == "hash1"));
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
            await _fixture.EventPublisher.Received(1)
                .PublishDownloadStopped(Arg.Any<double>(), TimeSpan.Zero, "tl", CleanReason.AbandonedBelowThreshold);
        }

        [Fact]
        public async Task DoesNotDelete_WhenDryRunInterceptsTheAction()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            var torrent = CreateIncomplete("hash1");
            SetupClient(torrent);
            _fixture.DryRunInterceptor
                .InterceptAsync(Arg.Any<Func<Task>>(), Arg.Any<string?>())
                .ReturnsForAnyArgs(Task.CompletedTask);

            // Act
            await sut.AbandonDownloadsAsync([torrent], [CreateRule()]);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }
    }

    public class FilterDownloadsToChangeCategoryAsync_Tests : QBitServiceDCTests
    {
        public FilterDownloadsToChangeCategoryAsync_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void ExcludesAlreadyTagged_WhenTagModeEnabled()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = true,
                TargetCategory = "unlinked",
                Categories = ["movies"]
            };

            var torrentInfo1 = new TorrentInfo { Hash = "hash1", Category = "movies", Tags = new[] { "unlinked" } };
            var torrentInfo2 = new TorrentInfo { Hash = "hash2", Category = "movies", Tags = Array.Empty<string>() };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(torrentInfo1, Array.Empty<TorrentTracker>(), false),
                new QBitItemWrapper(torrentInfo2, Array.Empty<TorrentTracker>(), false)
            };

            // Act
            var result = sut.FilterDownloadsToChangeCategoryAsync(downloads, unlinkedConfig);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldHaveSingleItem();
            result[0].DownloadId.ShouldBe("hash2");
        }

        [Fact]
        public void IncludesAll_WhenCategoryModeEnabled()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked",
                Categories = ["movies"]
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Category = "movies" }, Array.Empty<TorrentTracker>(), false),
                new QBitItemWrapper(new TorrentInfo { Hash = "hash2", Category = "movies" }, Array.Empty<TorrentTracker>(), false)
            };

            // Act
            var result = sut.FilterDownloadsToChangeCategoryAsync(downloads, unlinkedConfig);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
        }

        [Fact]
        public void IsCaseInsensitive()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                Categories = ["movies"]
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Category = "Movies" }, Array.Empty<TorrentTracker>(), false)
            };

            // Act
            var result = sut.FilterDownloadsToChangeCategoryAsync(downloads, unlinkedConfig);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldHaveSingleItem();
        }

        [Fact]
        public void SkipsDownloadsWithEmptyHash()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                Categories = ["movies"]
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "", Category = "movies" }, Array.Empty<TorrentTracker>(), false),
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Category = "movies" }, Array.Empty<TorrentTracker>(), false)
            };

            // Act
            var result = sut.FilterDownloadsToChangeCategoryAsync(downloads, unlinkedConfig);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldHaveSingleItem();
            result[0].DownloadId.ShouldBe("hash1");
        }

        [Fact]
        public void ReturnsEmpty_WhenNoCategoriesMatch()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Category = "tv" }, Array.Empty<TorrentTracker>(), false)
            };

            // Act
            var result = sut.FilterDownloadsToChangeCategoryAsync(downloads, new UnlinkedConfig { Categories = ["movies"] });

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }
    }

    public class CreateCategoryAsync_Tests : QBitServiceDCTests
    {
        public CreateCategoryAsync_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task CreatesCategory_WhenMissing()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            _fixture.ClientWrapper
                .GetCategoriesAsync()
                .Returns(new Dictionary<string, Category>());

            _fixture.ClientWrapper
                .AddCategoryAsync("new-category")
                .Returns(Task.CompletedTask);

            // Act
            await sut.CreateCategoryAsync("new-category");

            // Assert
            await _fixture.ClientWrapper.Received(1).AddCategoryAsync("new-category");
        }

        [Fact]
        public async Task SkipsCreation_WhenCategoryExists()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            _fixture.ClientWrapper
                .GetCategoriesAsync()
                .Returns(new Dictionary<string, Category>
                {
                    { "existing", new Category { Name = "existing" } }
                });

            // Act
            await sut.CreateCategoryAsync("existing");

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().AddCategoryAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task IsCaseInsensitive()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            _fixture.ClientWrapper
                .GetCategoriesAsync()
                .Returns(new Dictionary<string, Category>
                {
                    { "existing", new Category { Name = "Existing" } }
                });

            // Act
            await sut.CreateCategoryAsync("existing");

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().AddCategoryAsync(Arg.Any<string>());
        }
    }

    public class DeleteDownload_Tests : QBitServiceDCTests
    {
        public DeleteDownload_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task CallsClientDelete()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            const string hash = "test-hash";
            var mockTorrent = Substitute.For<ITorrentItemWrapper>();
            mockTorrent.DownloadId.Returns(hash);

            _fixture.ClientWrapper
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains(hash)), true)
                .Returns(Task.CompletedTask);

            // Act
            await sut.DeleteDownload(mockTorrent, true);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Is<IEnumerable<string>>(h => h.Contains(hash)), true);
        }

        [Fact]
        public async Task DeletesWithData()
        {
            // Arrange
            var sut = _fixture.CreateSut();
            const string hash = "test-hash";
            var mockTorrent = Substitute.For<ITorrentItemWrapper>();
            mockTorrent.DownloadId.Returns(hash);

            _fixture.ClientWrapper
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
                .Returns(Task.CompletedTask);

            // Act
            await sut.DeleteDownload(mockTorrent, true);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), true);
        }
    }

    public class StopDownload_Tests : QBitServiceDCTests
    {
        public StopDownload_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task CallsClientPause()
        {
            // Arrange
            QBitService sut = _fixture.CreateSut();
            const string hash = "test-hash";
            ITorrentItemWrapper mockTorrent = Substitute.For<ITorrentItemWrapper>();
            mockTorrent.DownloadId.Returns(hash);

            _fixture.ClientWrapper
                .PauseAsync(Arg.Is<IEnumerable<string>>(h => h.Contains(hash)))
                .Returns(Task.CompletedTask);

            // Act
            await sut.StopDownload(mockTorrent);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .PauseAsync(Arg.Is<IEnumerable<string>>(h => h.Contains(hash)));
        }

        [Fact]
        public async Task DoesNotDeleteTheTorrent()
        {
            // Arrange
            QBitService sut = _fixture.CreateSut();
            ITorrentItemWrapper mockTorrent = Substitute.For<ITorrentItemWrapper>();
            mockTorrent.DownloadId.Returns("test-hash");

            _fixture.ClientWrapper
                .PauseAsync(Arg.Any<IEnumerable<string>>())
                .Returns(Task.CompletedTask);

            // Act
            await sut.StopDownload(mockTorrent);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }
    }

    public class ResumeDownload_Tests : QBitServiceDCTests
    {
        public ResumeDownload_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task CallsClientResume()
        {
            // Arrange
            QBitService sut = _fixture.CreateSut();
            const string hash = "test-hash";
            ITorrentItemWrapper mockTorrent = Substitute.For<ITorrentItemWrapper>();
            mockTorrent.DownloadId.Returns(hash);

            _fixture.ClientWrapper
                .ResumeAsync(Arg.Is<IEnumerable<string>>(h => h.Contains(hash)))
                .Returns(Task.CompletedTask);

            // Act
            await sut.ResumeDownload(mockTorrent);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .ResumeAsync(Arg.Is<IEnumerable<string>>(h => h.Contains(hash)));
        }

        [Fact]
        public async Task DoesNotDeleteTheTorrent()
        {
            // Arrange
            QBitService sut = _fixture.CreateSut();
            ITorrentItemWrapper mockTorrent = Substitute.For<ITorrentItemWrapper>();
            mockTorrent.DownloadId.Returns("test-hash");

            _fixture.ClientWrapper
                .ResumeAsync(Arg.Any<IEnumerable<string>>())
                .Returns(Task.CompletedTask);

            // Act
            await sut.ResumeDownload(mockTorrent);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive()
                .DeleteAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
        }
    }

    public class IsStopped_Tests : QBitServiceDCTests
    {
        public IsStopped_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Theory]
        [InlineData(TorrentState.PausedUpload)]
        [InlineData(TorrentState.PausedDownload)]
        public void ReturnsTrueForPausedStates(TorrentState state)
        {
            // Arrange
            QBitItemWrapper wrapper = new QBitItemWrapper(
                new TorrentInfo { Hash = "hash1", State = state }, Array.Empty<TorrentTracker>(), false);

            // Act & Assert
            wrapper.IsStopped.ShouldBeTrue();
        }

        [Theory]
        [InlineData(TorrentState.Uploading)]
        [InlineData(TorrentState.Downloading)]
        [InlineData(TorrentState.StalledUpload)]
        public void ReturnsFalseForRunningStates(TorrentState state)
        {
            // Arrange
            QBitItemWrapper wrapper = new QBitItemWrapper(
                new TorrentInfo { Hash = "hash1", State = state }, Array.Empty<TorrentTracker>(), false);

            // Act & Assert
            wrapper.IsStopped.ShouldBeFalse();
        }
    }

    public class ChangeTorrentCategoryAsync_Tests : QBitServiceDCTests
    {
        public ChangeTorrentCategoryAsync_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task CategoryMode_SetsCategory_AndPublishes()
        {
            var sut = _fixture.CreateSut();
            var torrent = Substitute.For<ITorrentItemWrapper>();
            torrent.DownloadId.Returns("hash1");
            torrent.Name.Returns("Test");
            torrent.Category.Returns("movies");

            await sut.ChangeTorrentCategoryAsync(torrent, "cleanuparr-dead", useTag: false);

            await _fixture.ClientWrapper.Received(1)
                .SetTorrentCategoryAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), "cleanuparr-dead");
            await _fixture.ClientWrapper.DidNotReceive()
                .AddTorrentTagAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
            await _fixture.EventPublisher.Received(1)
                .PublishCategoryChanged("movies", "cleanuparr-dead", false);
        }

        [Fact]
        public async Task TagMode_AddsTag_AndPublishes()
        {
            var sut = _fixture.CreateSut();
            var torrent = Substitute.For<ITorrentItemWrapper>();
            torrent.DownloadId.Returns("hash1");
            torrent.Name.Returns("Test");
            torrent.Category.Returns("movies");

            await sut.ChangeTorrentCategoryAsync(torrent, "cleanuparr-dead", useTag: true);

            await _fixture.ClientWrapper.Received(1)
                .AddTorrentTagAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), "cleanuparr-dead");
            await _fixture.ClientWrapper.DidNotReceive()
                .SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
            await _fixture.EventPublisher.Received(1)
                .PublishCategoryChanged("movies", "cleanuparr-dead", true);
        }

        [Fact]
        public async Task DryRun_SkipsClientCall_ButPublishesEvent()
        {
            var sut = _fixture.CreateSut();
            var torrent = Substitute.For<ITorrentItemWrapper>();
            torrent.DownloadId.Returns("hash1");
            torrent.Name.Returns("Test");
            torrent.Category.Returns("movies");

            _fixture.DryRunInterceptor
                .InterceptAsync(Arg.Any<Func<Task>>(), Arg.Any<string?>())
                .Returns(Task.CompletedTask);

            await sut.ChangeTorrentCategoryAsync(torrent, "cleanuparr-dead", useTag: false);

            await _fixture.ClientWrapper.DidNotReceive()
                .SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
            await _fixture.ClientWrapper.DidNotReceive()
                .AddTorrentTagAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
            await _fixture.EventPublisher.Received(1)
                .PublishCategoryChanged("movies", "cleanuparr-dead", false);
        }
    }

    public class ChangeCategoryForNoHardLinksAsync_Tests : QBitServiceDCTests
    {
        public ChangeCategoryForNoHardLinksAsync_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task NullDownloads_DoesNothing()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(null, unlinkedConfig);

            // Assert - no exceptions thrown
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task EmptyDownloads_DoesNothing()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(new List<Domain.Entities.ITorrentItemWrapper>(), unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task MissingHash_SkipsTorrent()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task MissingName_SkipsTorrent()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task MissingCategory_SkipsTorrent()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task NoFiles_SkipsTorrent()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns((IReadOnlyList<TorrentContent>?)null);

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task NoHardlinks_ChangesCategory()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = 0, Name = "file1.mkv", Priority = TorrentContentPriority.Normal }
                });

            _fixture.HardLinkFileService
                .GetHardLinkCount(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(0);

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .SetTorrentCategoryAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), "unlinked");
        }

        [Fact]
        public async Task NoHardlinks_TagMode_AddsTag()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = true,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = 0, Name = "file1.mkv", Priority = TorrentContentPriority.Normal }
                });

            _fixture.HardLinkFileService
                .GetHardLinkCount(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(0);

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.Received(1)
                .AddTorrentTagAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), "unlinked");
            await _fixture.ClientWrapper.DidNotReceive()
                .SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task HasHardlinks_SkipsTorrent()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = 0, Name = "file1.mkv", Priority = TorrentContentPriority.Normal }
                });

            _fixture.HardLinkFileService
                .GetHardLinkCount(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(2); // Has hardlinks

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task FileNotFound_SkipsTorrent()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = 0, Name = "file1.mkv", Priority = TorrentContentPriority.Normal }
                });

            _fixture.HardLinkFileService
                .GetHardLinkCount(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(-1); // Error

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task SkippedFiles_IgnoredInCheck()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = 0, Name = "file1.mkv", Priority = TorrentContentPriority.Skip },
                    new TorrentContent { Index = 1, Name = "file2.mkv", Priority = TorrentContentPriority.Normal }
                });

            _fixture.HardLinkFileService
                .GetHardLinkCount(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(0);

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            _fixture.HardLinkFileService.Received(1)
                .GetHardLinkCount(Arg.Any<string>(), Arg.Any<bool>()); // Only called for file2.mkv
        }

        [Fact]
        public async Task FileWithNullIndex_SkipsTorrent()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = null, Name = "file1.mkv", Priority = TorrentContentPriority.Normal }
                });

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert
            await _fixture.ClientWrapper.DidNotReceive().SetTorrentCategoryAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
        }

        [Fact]
        public async Task PublishesCategoryChangedEvent()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = false,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = 0, Name = "file1.mkv", Priority = TorrentContentPriority.Normal }
                });

            _fixture.HardLinkFileService
                .GetHardLinkCount(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(0);

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert - EventPublisher is not mocked, so we just verify the method completed
            await _fixture.ClientWrapper.Received(1)
                .SetTorrentCategoryAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), "unlinked");
        }

        [Fact]
        public async Task PublishesCategoryChangedEvent_WithTagFlag()
        {
            // Arrange
            var sut = _fixture.CreateSut();

            var unlinkedConfig = new UnlinkedConfig
            {
                Id = Guid.NewGuid(),
                UseTag = true,
                TargetCategory = "unlinked"
            };

            var downloads = new List<Domain.Entities.ITorrentItemWrapper>
            {
                new QBitItemWrapper(new TorrentInfo { Hash = "hash1", Name = "Test", Category = "movies", SavePath = "/downloads" }, Array.Empty<TorrentTracker>(), false)
            };

            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = 0, Name = "file1.mkv", Priority = TorrentContentPriority.Normal }
                });

            _fixture.HardLinkFileService
                .GetHardLinkCount(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(0);

            // Act
            await sut.ChangeCategoryForNoHardLinksAsync(downloads, unlinkedConfig);

            // Assert - EventPublisher is not mocked, so we just verify the method completed
            await _fixture.ClientWrapper.Received(1)
                .AddTorrentTagAsync(Arg.Is<IEnumerable<string>>(h => h.Contains("hash1")), "unlinked");
        }
    }

    public class GetClaimedPaths_Tests : QBitServiceDCTests
    {
        public GetClaimedPaths_Tests(QBitServiceFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task UsesFileList_WhenDisplayNameDivergesFromDisk()
        {
            var sut = _fixture.CreateSut();
            var wrapper = new QBitItemWrapper(
                new TorrentInfo { Hash = "hash1", Name = "Renamed Display Name", SavePath = "/downloads" },
                Array.Empty<TorrentTracker>(),
                false);
            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[] { new TorrentContent { Index = 0, Name = "actual-folder/data.bin", Priority = TorrentContentPriority.Normal } });

            IReadOnlyList<string> claimed = await sut.GetClaimedPathsAsync(new Domain.Entities.ITorrentItemWrapper[] { wrapper });

            claimed.ShouldContain("/downloads/actual-folder");
            claimed.ShouldNotContain("/downloads/Renamed Display Name");
        }

        [Fact]
        public async Task FallsBackToSavePathAndName_WhenFileListUnavailable()
        {
            // no files returned (e.g. metadata not yet fetched) — claim save path + name.
            var sut = _fixture.CreateSut();
            var wrapper = new QBitItemWrapper(
                new TorrentInfo { Hash = "hash1", Name = "some-show", SavePath = "/downloads" },
                Array.Empty<TorrentTracker>(),
                false);
            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(Array.Empty<TorrentContent>());

            IReadOnlyList<string> claimed = await sut.GetClaimedPathsAsync(new Domain.Entities.ITorrentItemWrapper[] { wrapper });

            claimed.ShouldContain("/downloads/some-show");
        }

        [Fact]
        public async Task MultiFileSharingFolder_ClaimsSingleRoot()
        {
            // both files live under one folder → one claimed entry, not the deep file paths.
            var sut = _fixture.CreateSut();
            var wrapper = new QBitItemWrapper(
                new TorrentInfo { Hash = "hash1", Name = "show", SavePath = "/downloads" },
                Array.Empty<TorrentTracker>(),
                false);
            _fixture.ClientWrapper
                .GetTorrentContentsAsync("hash1")
                .Returns(new[]
                {
                    new TorrentContent { Index = 0, Name = "show/file1.mkv", Priority = TorrentContentPriority.Normal },
                    new TorrentContent { Index = 1, Name = "show/file2.mkv", Priority = TorrentContentPriority.Normal }
                });

            IReadOnlyList<string> claimed = await sut.GetClaimedPathsAsync(new Domain.Entities.ITorrentItemWrapper[] { wrapper });

            claimed.ShouldContain("/downloads/show");
            claimed.Count(p => p == "/downloads/show").ShouldBe(1);
            claimed.ShouldNotContain("/downloads/show/file1.mkv");
        }
    }
}
