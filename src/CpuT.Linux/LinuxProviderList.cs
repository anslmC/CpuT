using CpuT.Core;
using CpuT.Linux.Providers;

namespace CpuT.Linux;

/// <summary>
/// Provides the default Linux provider set used by the platform composition layer.
/// </summary>
/// <remarks>
/// The list is ordered from the more specific known-driver path to the broader generic hwmon scan and is
/// intended for use by platform composition rather than as the primary consumer entry point.
/// </remarks>
public static class LinuxProviderList
{
    /// <summary>
    /// Gets the Linux provider list used for CPU temperature discovery.
    /// </summary>
    /// <returns>
    /// An ordered set of Linux-specific temperature providers used by the library.
    /// </returns>
    public static IReadOnlyList<ITemperatureProvider> GetProviders() =>
    [
        new HwmonKnownDriverTemperatureProvider(),
        new HwmonGenericScanTemperatureProvider()
    ];
}
