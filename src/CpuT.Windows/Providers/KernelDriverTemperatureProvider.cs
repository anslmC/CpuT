using CpuT.Core;

namespace CpuT.Windows.Providers;

internal sealed class KernelDriverTemperatureProvider : ITemperatureProvider
{
    public bool IsPlatformSupported() => OperatingSystem.IsWindows();

    public TemperatureResult TryRead() =>
        TemperatureResult.Unsupported("No bundled Windows kernel driver telemetry source is available.");

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TryRead());
}
