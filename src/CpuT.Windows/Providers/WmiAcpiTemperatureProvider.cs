using CpuT.Core;

namespace CpuT.Windows.Providers;

internal sealed class WmiAcpiTemperatureProvider : ITemperatureProvider
{
    public bool IsPlatformSupported() => OperatingSystem.IsWindows();

    public TemperatureResult TryRead() =>
        TemperatureResult.Unsupported("The Windows WMI/ACPI provider is not implemented yet.");

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TryRead());
}
