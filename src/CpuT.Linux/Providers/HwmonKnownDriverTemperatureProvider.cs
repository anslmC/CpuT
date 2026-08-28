using CpuT.Core;

namespace CpuT.Linux.Providers;

internal sealed class HwmonKnownDriverTemperatureProvider : ITemperatureProvider
{
    private readonly string hwmonRoot;

    public HwmonKnownDriverTemperatureProvider(string hwmonRoot = "/sys/class/hwmon")
    {
        this.hwmonRoot = hwmonRoot;
    }

    public bool IsPlatformSupported() => OperatingSystem.IsLinux();

    public TemperatureResult TryRead()
    {
        if (!Directory.Exists(hwmonRoot))
        {
            return TemperatureResult.Unsupported("The Linux hwmon filesystem is unavailable.");
        }

        IEnumerable<string> devicePaths;
        try
        {
            devicePaths = Directory.EnumerateDirectories(hwmonRoot).OrderBy(path => path).ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            return TemperatureResult.Failed(
                "Access to the Linux hwmon filesystem was denied.",
                TemperatureFailureReason.AccessDenied);
        }
        catch (IOException)
        {
            return TemperatureResult.Failed(
                "An I/O error occurred while scanning the Linux hwmon filesystem.",
                TemperatureFailureReason.ProviderError);
        }

        foreach (var devicePath in devicePaths)
        {
            try
            {
                if (HwmonTemperatureReader.IsExcludedDevice(devicePath))
                {
                    continue;
                }

                var result = HwmonTemperatureReader.TryReadCpuTemperature(devicePath, requireKnownDriver: true);
                if (result.Failure is not null)
                {
                    return result.Failure;
                }

                if (result.Reading is not null)
                {
                    return TemperatureResult.Valid(result.Reading);
                }
            }
            catch (HwmonTemperatureReader.HwmonReadException exception)
            {
                return exception.Result;
            }
        }

        return TemperatureResult.Unsupported("No supported Linux CPU hwmon driver was found.");
    }

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(TryRead());
    }
}