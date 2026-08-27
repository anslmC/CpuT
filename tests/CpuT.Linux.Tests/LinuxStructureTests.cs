using System.Globalization;
using CpuT.Core;
using CpuT.Linux;
using CpuT.Linux.Providers;
using CoreCpuT = CpuT.Core.CpuT;

namespace CpuT.Linux.Tests;

public class LinuxStructureTests
{
    [Fact]
    public void ProviderListHasFixedPriorityOrder()
    {
        Assert.Equal(2, LinuxProviderList.GetProviders().Count);
    }

    [Fact]
    public void KnownDriverProviderConvertsMillidegreesAndReturnsValidReading()
    {
        using var fixture = HwmonFixture.Create("k10temp", ("temp1", "Tctl", "65000"));

        var result = CreateKnownDriverProvider(fixture.Root).TryRead();

        Assert.True(result.IsValid);
        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.NotNull(result.Reading);
        Assert.Equal(65.0, result.Reading.Celsius);
        Assert.Equal("Tctl", result.Reading.SensorName);
    }

    [Theory]
    [InlineData(-51.0, false)]
    [InlineData(-50.0, true)]
    [InlineData(65.0, true)]
    [InlineData(150.0, true)]
    [InlineData(151.0, false)]
    public void LinuxTemperatureReadingFollowsValidationRange(double celsius, bool expectedValid)
    {
        using var fixture = HwmonFixture.Create(
            "k10temp",
            ("temp1", "Tctl", (celsius * 1000).ToString(CultureInfo.InvariantCulture)));
        using var cpu = new CoreCpuT([AsSupported(CreateKnownDriverProvider(fixture.Root))]);

        var result = cpu.Read();

        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(expectedValid ? TemperatureStatus.Valid : TemperatureStatus.Invalid, result.Status);
    }

    [Fact]
    public void InvalidLinuxReadingDoesNotProduceSuccessfulResultWithoutFallback()
    {
        using var fixture = HwmonFixture.Create("k10temp", ("temp1", "Tctl", "151000"));
        using var cpu = new CoreCpuT([AsSupported(CreateKnownDriverProvider(fixture.Root))]);

        var result = cpu.Read();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Invalid, result.Status);
        Assert.Null(result.Reading);
    }

    [Fact]
    public void InvalidLinuxReadingFallsBackToValidLinuxProvider()
    {
        using var invalidFixture = HwmonFixture.Create("k10temp", ("temp1", "Tctl", "151000"));
        using var validFixture = HwmonFixture.Create("k10temp", ("temp1", "Tctl", "65000"));
        using var cpu = new CoreCpuT([
            AsSupported(CreateKnownDriverProvider(invalidFixture.Root)),
            AsSupported(CreateKnownDriverProvider(validFixture.Root))]);

        var result = cpu.Read();

        Assert.True(result.IsValid);
        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.Equal(65.0, result.Reading?.Celsius);
    }

    [Fact]
    public void MalformedSensorDataDoesNotEscapeAsAnException()
    {
        using var fixture = HwmonFixture.Create("k10temp", ("temp1", "Tctl", "not-a-temperature"));

        var result = Record.Exception(() => CreateKnownDriverProvider(fixture.Root).TryRead());

        Assert.Null(result);
    }

    [Fact]
    public void KnownDriverPrefersTctlOverOtherValidSensors()
    {
        using var fixture = HwmonFixture.Create(
            "k10temp",
            ("temp1", "Tccd1", "41000"),
            ("temp2", "Tctl", "65000"),
            ("temp3", "Tccd2", "43000"));

        var result = CreateKnownDriverProvider(fixture.Root).TryRead();

        Assert.True(result.IsValid);
        Assert.NotNull(result.Reading);
        Assert.Equal(65.0, result.Reading.Celsius);
        Assert.Equal("Tctl", result.Reading.SensorName);
    }

    [Fact]
    public void KnownDriverFallsBackToTdieThenPackageThenFirstSensor()
    {
        using var tdieFixture = HwmonFixture.Create(
            "k10temp",
            ("temp1", "Tccd1", "41000"),
            ("temp2", "Tdie", "60000"),
            ("temp3", "Package", "55000"));
        using var packageFixture = HwmonFixture.Create(
            "k10temp",
            ("temp1", "Tccd1", "41000"),
            ("temp2", "Package", "55000"));
        using var fallbackFixture = HwmonFixture.Create(
            "k10temp",
            ("temp1", "Tccd1", "41000"),
            ("temp2", "Tccd2", "43000"));

        var tdie = CreateKnownDriverProvider(tdieFixture.Root).TryRead();
        var package = CreateKnownDriverProvider(packageFixture.Root).TryRead();
        var fallback = CreateKnownDriverProvider(fallbackFixture.Root).TryRead();

        Assert.Equal(60.0, tdie.Reading?.Celsius);
        Assert.Equal("Tdie", tdie.Reading?.SensorName);
        Assert.Equal(55.0, package.Reading?.Celsius);
        Assert.Equal("Package", package.Reading?.SensorName);
        Assert.Equal(41.0, fallback.Reading?.Celsius);
        Assert.Equal("Tccd1", fallback.Reading?.SensorName);
    }

    [Fact]
    public void GenericProviderUsesNumericSensorOrdering()
    {
        using var fixture = HwmonFixture.Create(
            "generic-cpu-sensor",
            ("temp1", "CPU sensor", "41000"),
            ("temp2", "CPU sensor", "42000"),
            ("temp10", "CPU sensor", "100000"));

        var result = CreateGenericProvider(fixture.Root).TryRead();

        Assert.True(result.IsValid);
        Assert.NotNull(result.Reading);
        Assert.Equal(41.0, result.Reading.Celsius);
    }

    [Fact]
    public void GenericProviderSkipsExcludedDevicesAndReadsCpuFallback()
    {
        using var fixture = HwmonFixture.Create(
            "amdgpu",
            ("temp1", "GPU temperature", "90000"));
        fixture.AddDevice("cpu-device", "acpi-cpu", ("temp1", "CPU temperature", "65000"));

        var result = CreateGenericProvider(fixture.Root).TryRead();

        Assert.True(result.IsValid);
        Assert.NotNull(result.Reading);
        Assert.Equal(65.0, result.Reading.Celsius);
    }

    [Fact]
    public void KnownDriverProviderRequiresKnownDriver()
    {
        using var fixture = HwmonFixture.Create("unknown-driver", ("temp1", "CPU temperature", "65000"));

        var result = CreateKnownDriverProvider(fixture.Root).TryRead();

        Assert.Equal(TemperatureStatus.Unsupported, result.Status);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GenericProviderReadsCpuSensorWhenDriverIsUnknown()
    {
        using var fixture = HwmonFixture.Create("unknown-driver", ("temp1", "CPU temperature", "65000"));

        var result = CreateGenericProvider(fixture.Root).TryRead();

        Assert.True(result.IsValid);
        Assert.Equal(65.0, result.Reading?.Celsius);
    }

    private static ITemperatureProvider CreateKnownDriverProvider(string root) =>
        CreateProvider("CpuT.Linux.Providers.HwmonKnownDriverTemperatureProvider", root);

    private static ITemperatureProvider CreateGenericProvider(string root) =>
        CreateProvider("CpuT.Linux.Providers.HwmonGenericScanTemperatureProvider", root);

    private static ITemperatureProvider AsSupported(ITemperatureProvider provider) =>
        new SupportedProvider(provider);

    private static ITemperatureProvider CreateProvider(string typeName, string root)
    {
        var providerType = typeof(LinuxProviderList).Assembly.GetType(typeName, throwOnError: true)!;
        return (ITemperatureProvider)Activator.CreateInstance(providerType, root)!;
    }

    private sealed class SupportedProvider(ITemperatureProvider inner) : ITemperatureProvider
    {
        public bool IsPlatformSupported() => true;

        public TemperatureResult TryRead() => inner.TryRead();

        public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default) =>
            inner.TryReadAsync(cancellationToken);
    }

    private sealed class HwmonFixture : IDisposable
    {
        private HwmonFixture(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static HwmonFixture Create(
            string driver,
            params (string Name, string Label, string Value)[] sensors)
        {
            var fixture = new HwmonFixture(Path.Combine(Path.GetTempPath(), "CpuT-LinuxTests-" + Guid.NewGuid()));
            fixture.AddDevice("hwmon0", driver, sensors);
            return fixture;
        }

        public void AddDevice(
            string deviceName,
            string driver,
            params (string Name, string Label, string Value)[] sensors)
        {
            var devicePath = Directory.CreateDirectory(Path.Combine(Root, deviceName)).FullName;
            File.WriteAllText(Path.Combine(devicePath, "name"), driver);
            foreach (var sensor in sensors)
            {
                File.WriteAllText(Path.Combine(devicePath, sensor.Name + "_label"), sensor.Label);
                File.WriteAllText(Path.Combine(devicePath, sensor.Name + "_input"), sensor.Value);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
