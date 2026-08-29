namespace CpuT.Core;

/// <summary>
/// Describes the overall outcome of a CPU temperature read.
/// </summary>
/// <remarks>
/// The values represent distinct consumer-visible states: a usable reading is available,
/// no reading is currently available, the environment is unsupported, the reading was
/// rejected as invalid, or the provider failed while attempting to read.
/// </remarks>
public enum TemperatureStatus
{
    /// <summary>
    /// A usable CPU temperature reading is available.
    /// </summary>
    Valid,

    /// <summary>
    /// The system does not currently expose a usable CPU temperature reading.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The current environment does not provide a supported temperature source for this library.
    /// </summary>
    Unsupported,

    /// <summary>
    /// A value was encountered but it was rejected as unusable or out of range.
    /// </summary>
    Invalid,

    /// <summary>
    /// The read attempt encountered a failure condition and may include more detail in <see cref="TemperatureResult.FailureReason"/>.
    /// </summary>
    Failed
}
