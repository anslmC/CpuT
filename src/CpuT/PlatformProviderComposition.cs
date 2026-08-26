using CpuT.Core;
using CpuT.Linux;
using CpuT.Windows;

namespace CpuT;

public static class CpuT
{
    public static TemperatureResult Read() => PlatformProviderComposition.Create().Read();

    public static Task<TemperatureResult> ReadAsync(CancellationToken cancellationToken = default) =>
        PlatformProviderComposition.Create().ReadAsync(cancellationToken);
}

public static class PlatformProviderComposition
{
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
