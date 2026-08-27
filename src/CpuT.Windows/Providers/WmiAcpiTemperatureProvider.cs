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
        catch (ManagementException ex)
        {
            if (ex.ErrorCode == ManagementStatus.AccessDenied)
            {
                return TemperatureResult.Failed(
                    "Access to Windows WMI/ACPI temperature telemetry was denied. Running with administrator privileges may resolve this.",
                    TemperatureFailureReason.AccessDenied);
            }

            if (ex.ErrorCode is ManagementStatus.NotFound or ManagementStatus.InvalidClass or ManagementStatus.NotSupported)
            {
                return TemperatureResult.Unsupported(
                    "The Windows WMI/ACPI thermal zone class is not available on this system.");
            }

            var message = ex.ErrorCode switch
            {
                ManagementStatus.InvalidNamespace =>
                    "The Windows WMI namespace required for ACPI temperature telemetry is unavailable.",
                ManagementStatus.Timedout =>
                    "The Windows WMI/ACPI temperature query timed out.",
                _ =>
                    "Windows WMI/ACPI temperature telemetry encountered an unexpected error."
            };

            return TemperatureResult.Failed(message, TemperatureFailureReason.ProviderError);
        }
        catch (UnauthorizedAccessException)
        {
            return TemperatureResult.Failed(
                "Access to Windows WMI/ACPI temperature telemetry was denied. Running with administrator privileges may resolve this.",
                TemperatureFailureReason.AccessDenied);
        }
    }

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(TryRead());
    }
}