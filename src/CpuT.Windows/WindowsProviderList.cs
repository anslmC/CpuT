using CpuT.Core;
using CpuT.Windows.Providers;

namespace CpuT.Windows;

public static class WindowsProviderList
{
    public static IReadOnlyList<ITemperatureProvider> GetProviders() =>
    [
        new KernelDriverTemperatureProvider(),
        new WmiAcpiTemperatureProvider()
    ];
}
