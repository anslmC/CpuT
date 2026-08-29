using CpuT.Core;
using CpuT.Windows.Providers;

namespace CpuT.Windows;

/// <summary>
/// Provides the default Windows provider set used by the platform composition layer.
/// </summary>
/// <remarks>
/// The list is ordered from the built-in kernel-driver placeholder to the WMI/ACPI provider and is intended
/// for use by the library's platform composition rather than as a direct consumer entry point.
/// </remarks>
public static class WindowsProviderList
{
    /// <summary>
    /// Gets the Windows provider list used for CPU temperature discovery.
    /// </summary>
    /// <returns>
    /// An ordered set of Windows-specific temperature providers used by the library.
    /// </returns>
    public static IReadOnlyList<ITemperatureProvider> GetProviders() =>
    [
        new KernelDriverTemperatureProvider(),
        new WmiAcpiTemperatureProvider()
    ];
}
