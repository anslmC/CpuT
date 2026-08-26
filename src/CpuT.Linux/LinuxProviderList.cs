using CpuT.Core;
using CpuT.Linux.Providers;

namespace CpuT.Linux;

public static class LinuxProviderList
{
    public static IReadOnlyList<ITemperatureProvider> GetProviders() =>
    [
        new HwmonKnownDriverTemperatureProvider(),
        new HwmonGenericScanTemperatureProvider()
    ];
}
