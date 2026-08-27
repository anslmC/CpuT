using System.Globalization;
using CpuT.Core;

namespace CpuT.Linux.Providers;

internal static class HwmonTemperatureReader
{
    private static readonly string[] CpuDrivers = ["coretemp", "k10temp", "zenpower", "fam15h_power", "fam17h_power"];
    private static readonly string[] CpuTerms = ["cpu", "core", "package", "processor", "tdie", "tctl"];
    private static readonly string[] ExcludedTerms = ["gpu", "nvme", "drivetemp", "acpitz", "battery", "pch", "board"];

    public static bool IsExcludedDevice(string devicePath)
    {
        var metadata = ReadMetadata(devicePath);
        return ContainsTerm(metadata, ExcludedTerms);
    }

    public static TemperatureReading? TryReadCpuTemperature(string devicePath, bool requireKnownDriver)
    {
        var metadata = ReadMetadata(devicePath);
        var driver = ReadFirstLine(Path.Combine(devicePath, "name"));
        var isKnownDriver = driver is not null && CpuDrivers.Contains(driver, StringComparer.OrdinalIgnoreCase);

        if (requireKnownDriver && !isKnownDriver || !requireKnownDriver && !isKnownDriver && !ContainsTerm(metadata, CpuTerms))
        {
            return null;
        }

        IEnumerable<string> inputPaths;
        try
        {
            inputPaths = Directory.EnumerateFiles(devicePath, "temp*_input").OrderBy(path => path).ToArray();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        foreach (var inputPath in inputPaths)
        {
            var raw = ReadFirstLine(inputPath);
            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var millidegrees))
            {
                continue;
            }

            var celsius = millidegrees / 1000d;
            var labelPath = Path.Combine(devicePath, Path.GetFileNameWithoutExtension(inputPath).Replace("_input", "_label", StringComparison.Ordinal));
            var label = ReadFirstLine(labelPath);
            var sensorName = label ?? driver ?? Path.GetFileName(devicePath);

            if (ContainsTerm(sensorName, ExcludedTerms) || !isKnownDriver && !ContainsTerm(sensorName, CpuTerms) && !ContainsTerm(metadata, CpuTerms))
            {
                continue;
            }

            return new TemperatureReading(celsius, DateTimeOffset.UtcNow, sensorName);
        }

        return null;
    }

    private static string ReadMetadata(string devicePath) =>
        string.Join(' ',
            ReadFirstLine(Path.Combine(devicePath, "name")),
            ReadFirstLine(Path.Combine(devicePath, "modalias")),
            ReadLabels(devicePath));

    private static string ReadLabels(string devicePath) =>
        string.Join(' ', EnumerateLabelPaths(devicePath)
            .Select(ReadFirstLine)
            .Where(label => label is not null));

    private static IEnumerable<string> EnumerateLabelPaths(string devicePath)
    {
        try
        {
            return Directory.EnumerateFiles(devicePath, "temp*_label").OrderBy(path => path).ToArray();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
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
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}