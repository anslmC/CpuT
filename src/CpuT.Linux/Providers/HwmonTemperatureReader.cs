using System.Globalization;
using CpuT.Core;

namespace CpuT.Linux.Providers;

internal static class HwmonTemperatureReader
{
    private static readonly string[] CpuDrivers = ["coretemp", "k10temp", "zenpower", "fam15h_power", "fam17h_power"];
    private static readonly string[] CpuTerms = ["cpu", "core", "package", "processor", "tdie", "tctl"];
    private static readonly string[] ExcludedTerms = ["gpu", "nvme", "drivetemp", "acpitz", "battery", "pch", "board"];

    public static bool IsExcludedDevice(string devicePath, Func<string, string?>? readFirstLine = null)
    {
        var metadata = ReadMetadata(devicePath, readFirstLine ?? ReadFirstLine);
        return ContainsTerm(metadata, ExcludedTerms);
    }

    public static (TemperatureReading? Reading, TemperatureResult? Failure) TryReadCpuTemperature(
        string devicePath,
        bool requireKnownDriver,
        Func<string, string?>? readFirstLine = null)
    {
        try
        {
            return TryReadCpuTemperatureCore(devicePath, requireKnownDriver, readFirstLine);
        }
        catch (HwmonReadException exception)
        {
            return (null, exception.Result);
        }
    }

    private static (TemperatureReading? Reading, TemperatureResult? Failure) TryReadCpuTemperatureCore(
        string devicePath,
        bool requireKnownDriver,
        Func<string, string?>? readFirstLine = null)
    {
        var lineReader = readFirstLine ?? ReadFirstLine;
        readFirstLine = path => ReadClassified(lineReader, path);
        var metadata = ReadMetadata(devicePath, readFirstLine);
        var driver = readFirstLine(Path.Combine(devicePath, "name"));
        var isKnownDriver = driver is not null && CpuDrivers.Contains(driver, StringComparer.OrdinalIgnoreCase);

        if (requireKnownDriver && !isKnownDriver || !requireKnownDriver && !isKnownDriver && !ContainsTerm(metadata, CpuTerms))
        {
            return (null, null);
        }

        List<string> inputPaths;
        try
        {
            inputPaths = Directory.EnumerateFiles(devicePath, "temp*_input").ToList();
        }
        catch (IOException exception) when (IsAccessDenied(exception))
        {
            return (null, AccessDeniedResult());
        }
        catch (UnauthorizedAccessException)
        {
            return (null, AccessDeniedResult());
        }

        var candidates = new List<(string InputPath, string? Label, int Priority, int Index)>();

        foreach (var inputPath in inputPaths)
        {
            var labelPath = Path.Combine(devicePath, Path.GetFileNameWithoutExtension(inputPath).Replace("_input", "_label", StringComparison.Ordinal));
            var label = readFirstLine(labelPath);
            var sensorName = label ?? driver ?? Path.GetFileName(devicePath);

            if (ContainsTerm(sensorName, ExcludedTerms) || !isKnownDriver && !ContainsTerm(sensorName, CpuTerms) && !ContainsTerm(metadata, CpuTerms))
            {
                continue;
            }

            candidates.Add((inputPath, label, GetLabelPriority(label), ExtractSensorIndex(inputPath)));
        }

        foreach (var candidate in candidates.OrderBy(c => c.Priority).ThenBy(c => c.Index))
        {
            var raw = readFirstLine(candidate.InputPath);
            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var millidegrees))
            {
                continue;
            }

            var celsius = millidegrees / 1000d;
            var sensorName = candidate.Label ?? driver ?? Path.GetFileName(devicePath);
            return (new TemperatureReading(celsius, DateTimeOffset.UtcNow, sensorName), null);
        }

        return (null, null);
    }

    private static int GetLabelPriority(string? label)
    {
        if (label is null)
        {
            return 3;
        }

        if (label.Contains("tctl", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (label.Contains("tdie", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (label.Contains("package", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 3;
    }

    private static int ExtractSensorIndex(string inputPath)
    {
        var fileName = Path.GetFileName(inputPath);
        var digits = new string(fileName.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            ? index
            : int.MaxValue;
    }

    private static string ReadMetadata(string devicePath, Func<string, string?> readFirstLine) =>
        string.Join(' ',
            readFirstLine(Path.Combine(devicePath, "name")),
            readFirstLine(Path.Combine(devicePath, "modalias")),
            ReadLabels(devicePath, readFirstLine));

    private static string ReadLabels(string devicePath, Func<string, string?> readFirstLine) =>
        string.Join(' ', EnumerateLabelPaths(devicePath)
            .Select(readFirstLine)
            .Where(label => label is not null));

    private static IEnumerable<string> EnumerateLabelPaths(string devicePath)
    {
        try
        {
            return Directory.EnumerateFiles(devicePath, "temp*_label").OrderBy(path => path).ToArray();
        }
        catch (IOException exception) when (IsAccessDenied(exception))
        {
            throw new HwmonReadException(AccessDeniedResult());
        }
        catch (UnauthorizedAccessException)
        {
            throw new HwmonReadException(AccessDeniedResult());
        }
    }

    private static bool ContainsTerm(string value, IEnumerable<string> terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static string? ReadFirstLine(string path)
    {
        try
        {
            return File.ReadLines(path).FirstOrDefault()?.Trim();
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            throw new HwmonReadException(
                TemperatureResult.Failed(
                    "Access to Linux hwmon temperature telemetry was denied. Adjusting udev rules or group membership may resolve this.",
                    TemperatureFailureReason.AccessDenied));
        }
        catch (IOException exception) when (IsAccessDenied(exception))
        {
            throw new HwmonReadException(
                TemperatureResult.Failed(
                    "Access to Linux hwmon temperature telemetry was denied. Adjusting udev rules or group membership may resolve this.",
                    TemperatureFailureReason.AccessDenied));
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool IsAccessDenied(IOException exception) =>
        (exception.HResult & 0xFFFF) is 5 or 13;

    private static string? ReadClassified(Func<string, string?> readFirstLine, string path)
    {
        try
        {
            return readFirstLine(path);
        }
        catch (UnauthorizedAccessException)
        {
            throw new HwmonReadException(AccessDeniedResult());
        }
        catch (IOException exception) when (IsAccessDenied(exception))
        {
                throw new HwmonReadException(AccessDeniedResult());
        }
    }

    private static TemperatureResult AccessDeniedResult() =>
        TemperatureResult.Failed(
            "Access to Linux hwmon temperature telemetry was denied. Adjusting udev rules or group membership may resolve this.",
            TemperatureFailureReason.AccessDenied);

    internal sealed class HwmonReadException(TemperatureResult result) : Exception
    {
        public TemperatureResult Result { get; } = result;
    }
}