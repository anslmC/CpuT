using CpuT.Core;

namespace CpuT.Linux.Providers;

internal sealed class HwmonGenericScanTemperatureProvider : ITemperatureProvider
{
    public bool IsPlatformSupported() => OperatingSystem.IsLinux();

    public TemperatureResult TryRead() =>
        TemperatureResult.Unsupported("The Linux generic hwmon provider is not implemented yet.");

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TryRead());
}
