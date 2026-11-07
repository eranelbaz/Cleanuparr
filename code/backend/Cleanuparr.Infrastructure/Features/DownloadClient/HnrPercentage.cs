namespace Cleanuparr.Infrastructure.Features.DownloadClient;

/// <summary>
/// Builds the hit-and-run safe downloaded percentage shared by the torrent wrappers.
/// </summary>
public static class HnrPercentage
{
    /// <summary>
    /// The higher of verified progress and all-time downloaded bytes over the wanted size, as 0-100.
    /// Missing data returns 100, so an unknown torrent is never treated as barely downloaded.
    /// </summary>
    /// <param name="progress">Verified progress over the wanted files, as a 0-1 fraction.</param>
    /// <param name="downloadedBytes">All-time downloaded bytes, wasted bytes included.</param>
    /// <param name="wantedSize">Size of the wanted files in bytes.</param>
    public static double From(double? progress, long? downloadedBytes, long? wantedSize)
    {
        if (progress is null || downloadedBytes is null || wantedSize is null or <= 0)
        {
            return 100;
        }

        double downloadedPercentage = downloadedBytes.Value * 100.0 / wantedSize.Value;

        return Math.Clamp(Math.Max(progress.Value * 100.0, downloadedPercentage), 0, 100);
    }
}
