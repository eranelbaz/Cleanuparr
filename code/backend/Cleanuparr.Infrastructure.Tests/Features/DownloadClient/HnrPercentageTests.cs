using Cleanuparr.Infrastructure.Features.DownloadClient;
using Shouldly;
using Xunit;

namespace Cleanuparr.Infrastructure.Tests.Features.DownloadClient;

public class HnrPercentageTests
{
    [Theory]
    [InlineData(0.05, 10L, 1000L, 5.0)]
    [InlineData(0.01, 80L, 1000L, 8.0)]
    [InlineData(0.0, 0L, 1000L, 0.0)]
    [InlineData(0.5, 5000L, 1000L, 100.0)]
    [InlineData(1.0, 1000L, 1000L, 100.0)]
    public void From_ReturnsHigherOfProgressAndDownloaded_ClampedTo100(double progress, long downloaded, long size, double expected)
    {
        // Act
        double result = HnrPercentage.From(progress, downloaded, size);

        // Assert
        result.ShouldBe(expected, 0.0001);
    }

    [Theory]
    [InlineData(null, 10L, 1000L)]
    [InlineData(0.01, null, 1000L)]
    [InlineData(0.01, 10L, null)]
    [InlineData(0.01, 10L, 0L)]
    [InlineData(0.01, 10L, -1L)]
    public void From_WhenDataMissing_Returns100(double? progress, long? downloaded, long? size)
    {
        // Act
        double result = HnrPercentage.From(progress, downloaded, size);

        // Assert
        result.ShouldBe(100);
    }
}
