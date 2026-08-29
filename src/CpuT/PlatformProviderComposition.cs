using CpuT.Core;
using CpuT.Linux;
using CpuT.Windows;

namespace CpuT;

/// <summary>
/// Provides a simple, shared entry point for CPU temperature reads using the platform-appropriate provider set.
/// </summary>
/// <remarks>
/// This facade reuses a single underlying monitor instance for the current process. It is intended as the
/// simplest consumer-facing API when a single shared temperature reader is sufficient.
/// </remarks>
public static class CpuT
{
    private static readonly global::CpuT.Core.CpuT shared = PlatformProviderComposition.Create();

    /// <summary>
    /// Reads the current CPU temperature synchronously using the shared platform monitor.
    /// </summary>
    /// <returns>
    /// The current temperature result, which may be valid, unavailable, unsupported, invalid, or failed.
    /// </returns>
    public static TemperatureResult Read() => shared.Read();

    /// <summary>
    /// Reads the current CPU temperature asynchronously using the shared platform monitor.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that resolves to the current temperature result.
    /// </returns>
    public static Task<TemperatureResult> ReadAsync(CancellationToken cancellationToken = default) =>
        shared.ReadAsync(cancellationToken);
}

/// <summary>
/// Creates the platform-specific consumer monitor used by the primary public façade.
/// </summary>
/// <remarks>
/// The composition logic selects the Windows or Linux provider list at runtime and returns a new
/// <see cref="global::CpuT.Core.CpuT"/> instance with the provider set for the current platform.
/// </remarks>
public static class PlatformProviderComposition
{
    /// <summary>
    /// Creates a new monitor instance configured for the current platform.
    /// </summary>
    /// <returns>
    /// A monitor that owns the active provider set for the current operating system.
    /// </returns>
    public static global::CpuT.Core.CpuT Create()
    {
        var providers = OperatingSystem.IsWindows()
            ? WindowsProviderList.GetProviders()
            : OperatingSystem.IsLinux()
                ? LinuxProviderList.GetProviders()
                : Array.Empty<ITemperatureProvider>();

        return new global::CpuT.Core.CpuT(providers);
    }
}
