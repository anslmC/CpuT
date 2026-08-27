using CpuT.Core;
using System.Globalization;
using System.Management;
using System.Runtime.Versioning;

namespace CpuT.Windows.Providers;

internal sealed class WmiAcpiTemperatureProvider : ITemperatureProvider
{
    public bool IsPlatformSupported() => OperatingSystem.IsWindows();

    public TemperatureResult TryRead() =>
        OperatingSystem.IsWindows()
            ? ReadWindows()
            : TemperatureResult.Unsupported("The Windows WMI/ACPI provider is unavailable on this platform.");

    [SupportedOSPlatform("windows")]
    private static TemperatureResult ReadWindows()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT InstanceName, CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");

            foreach (ManagementObject sensor in searcher.Get())
            {
                var instanceName = sensor["InstanceName"]?.ToString();
                if (string.IsNullOrWhiteSpace(instanceName) ||
                    !instanceName.Contains("cpu", StringComparison.OrdinalIgnoreCase) &&
                    !instanceName.Contains("proc", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!double.TryParse(sensor["CurrentTemperature"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var kelvinTenths))
                {
                    continue;
                }

                var celsius = kelvinTenths / 10d - 273.15d;
                return TemperatureResult.Valid(new TemperatureReading(celsius, DateTimeOffset.UtcNow, instanceName));
            }

            return TemperatureResult.Unsupported("Windows did not expose a CPU-identified ACPI thermal zone.");
        }
        catch (ManagementException)
        {
            return TemperatureResult.Failed(
                "Windows WMI/ACPI temperature telemetry encountered an operating-system error.",
                TemperatureFailureReason.ProviderError);
        }
        catch (UnauthorizedAccessException)
        {
            return TemperatureResult.Failed(
                "Access to Windows WMI/ACPI temperature telemetry was denied.",
                TemperatureFailureReason.AccessDenied);
        }
    }

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(TryRead());
    }
}
