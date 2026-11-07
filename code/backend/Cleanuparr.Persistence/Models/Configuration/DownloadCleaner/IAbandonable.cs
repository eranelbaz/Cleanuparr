using Cleanuparr.Domain.Exceptions;

namespace Cleanuparr.Persistence.Models.Configuration.DownloadCleaner;

/// <summary>
/// Marks a seeding rule that can abandon incomplete downloads, for clients that report hit-and-run safe progress.
/// rTorrent does not.
/// </summary>
public interface IAbandonable
{
    /// <summary>
    /// Incomplete downloads below this downloaded percentage are abandoned. Set to -1 to disable.
    /// </summary>
    double AbandonBelowPercentage { get; set; }

    /// <summary>
    /// Number of hours since the download was added before it can be abandoned. At least 1, so a download grabbed
    /// between the arr queue read and the abandon step is never dropped seconds after it was added.
    /// </summary>
    double AbandonAfterHours { get; set; }

    /// <summary>
    /// Whether this rule abandons incomplete downloads.
    /// </summary>
    bool IsAbandonEnabled => AbandonBelowPercentage > 0;

    /// <summary>
    /// Validates the abandon settings.
    /// </summary>
    void ValidateRule()
    {
        if (AbandonBelowPercentage is not -1 and (<= 0 or > 100))
        {
            throw new CustomValidationException("Abandon below percentage must be -1 (disabled) or greater than 0 and at most 100");
        }

        if (IsAbandonEnabled && AbandonAfterHours < 1)
        {
            throw new CustomValidationException("Abandon after hours must be at least 1");
        }
    }
}
