using CpuT.Core;

namespace CpuT.Linux.Providers;

internal sealed class HwmonKnownDriverTemperatureProvider : ITemperatureProvider
{
    public bool IsPlatformSupported() => OperatingSystem.IsLinux();

    public TemperatureResult TryRead() =>
        TemperatureResult.Unsupported("The Linux hwmon known-driver provider is not implemented yet.");

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TryRead());
}
