using CpuT.Core;

namespace CpuT.Windows.Providers;

internal sealed class KernelDriverTemperatureProvider : ITemperatureProvider
{
    public bool IsPlatformSupported() => OperatingSystem.IsWindows();

    public TemperatureResult TryRead() =>
        TemperatureResult.Unsupported("The Windows kernel driver provider is not implemented yet.");

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TryRead());
}
