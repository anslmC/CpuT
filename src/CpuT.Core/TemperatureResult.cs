namespace CpuT.Core;

/// <summary>
/// Represents the outcome of a CPU temperature read.
/// </summary>
/// <remarks>
/// The public positional constructor permits non-normalized combinations for compatibility with provider
/// results. Callers are encouraged to prefer the static factory methods on this type when creating a result
/// for a public or provider-facing contract.
/// </remarks>
public sealed record TemperatureResult(
    TemperatureStatus Status,
    TemperatureReading? Reading = null,
    string? Error = null,
    TemperatureFailureReason FailureReason = TemperatureFailureReason.None)
{
    /// <summary>
    /// Gets a value indicating whether the result currently represents a valid reading.
    /// </summary>
    /// <remarks>
    /// This convenience property is intended for consumers that want a simple success check.
    /// A valid result requires both a <see cref="TemperatureStatus.Valid"/> status and a non-null reading.
    /// </remarks>
    public bool IsValid => Status == TemperatureStatus.Valid && Reading is not null;

    /// <summary>
    /// Creates a successful temperature result with a usable reading.
    /// </summary>
    /// <param name="reading">The reading to expose as successful.</param>
    /// <returns>A valid result containing the supplied reading.</returns>
    public static TemperatureResult Valid(TemperatureReading reading) =>
        new(TemperatureStatus.Valid, reading);

    /// <summary>
    /// Creates a result indicating that no temperature could be obtained at the moment.
    /// </summary>
    /// <param name="error">Optional explanation for why no reading is currently available.</param>
    /// <returns>A result that indicates the reading is unavailable.</returns>
    public static TemperatureResult Unavailable(string? error = null) =>
        new(TemperatureStatus.Unavailable, Error: error);

    /// <summary>
    /// Creates a result indicating that the current environment does not support a usable temperature source.
    /// </summary>
    /// <param name="error">Optional explanation for the unsupported condition.</param>
    /// <returns>A result that indicates the platform or environment does not provide a supported source.</returns>
    public static TemperatureResult Unsupported(string? error = null) =>
        new(TemperatureStatus.Unsupported, Error: error);

    /// <summary>
    /// Creates a result indicating that a reading was observed but was rejected as unusable.
    /// </summary>
    /// <param name="error">Optional explanation for why the reading was rejected.</param>
    /// <returns>A result that indicates invalid data was obtained.</returns>
    public static TemperatureResult Invalid(string? error = null) =>
        new(TemperatureStatus.Invalid, Error: error);

    /// <summary>
    /// Creates a failed result that includes machine-readable failure information.
    /// </summary>
    /// <param name="error">Optional human-readable explanation for the failure.</param>
    /// <param name="failureReason">A machine-readable classification for the failure.</param>
    /// <returns>A failed result that can be inspected for both human and machine-readable failure information.</returns>
    public static TemperatureResult Failed(
        string? error = null,
        TemperatureFailureReason failureReason = TemperatureFailureReason.Unknown) =>
        new(TemperatureStatus.Failed, Error: error, FailureReason: failureReason);
}
