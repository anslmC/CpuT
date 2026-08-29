namespace CpuT.Core;

/// <summary>
/// Represents a temperature source that can determine whether it is applicable to the current platform
/// and can produce a <see cref="TemperatureResult"/> for a CPU temperature read.
/// </summary>
/// <remarks>
/// Implementations are expected to return a <see cref="TemperatureResult"/> instead of raw temperatures,
/// so consumers can handle support availability, failure, invalid values, and successful readings in a
/// uniform way.
/// </remarks>
public interface ITemperatureProvider
{
    /// <summary>
    /// Determines whether the provider is applicable to the current operating environment.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the provider can operate on the current platform; otherwise, <see langword="false"/>.
    /// </returns>
    bool IsPlatformSupported();

    /// <summary>
    /// Attempts to read a CPU temperature synchronously.
    /// </summary>
    /// <returns>
    /// The outcome of the read, including success, unsupported state, invalid data, or failure details.
    /// </returns>
    TemperatureResult TryRead();

    /// <summary>
    /// Attempts to read a CPU temperature asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the read before or during provider execution.
    /// </param>
    /// <returns>
    /// A task that resolves to the outcome of the read.
    /// </returns>
    Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default);
}
