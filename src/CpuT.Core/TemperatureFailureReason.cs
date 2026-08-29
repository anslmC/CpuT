namespace CpuT.Core;

/// <summary>
/// Classifies machine-readable failure information for a failed temperature read.
/// </summary>
/// <remarks>
/// These values describe the reason a read failed, when a failure was reported.
/// A non-<see cref="TemperatureStatus.Failed"/> result does not necessarily carry a failure reason.
/// </remarks>
public enum TemperatureFailureReason
{
    /// <summary>
    /// No failure reason is associated with the result.
    /// </summary>
    None,

    /// <summary>
    /// Access to the underlying temperature source was denied.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// The provider or platform reported a read or access error.
    /// </summary>
    ProviderError,

    /// <summary>
    /// The read is intentionally suppressed while discovery is temporarily cooling down.
    /// </summary>
    Cooldown,

    /// <summary>
    /// The failure reason could not be mapped to a more specific known classification.
    /// </summary>
    Unknown
}