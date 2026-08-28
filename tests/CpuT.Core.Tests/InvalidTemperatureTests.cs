using CpuT.Core;
using CoreCpuT = CpuT.Core.CpuT;

namespace CpuT.Core.Tests;

// TEMPORARY PHASE 3 TEST INFRASTRUCTURE
// Used only to verify temperature validation and fallback behavior.
// Not part of the production API.
public class InvalidTemperatureTests
{
    [Fact]
    public void ValidStatusWithoutReadingIsRejected()
    {
        using var cpu = new CoreCpuT([
            FakeTemperatureProvider.FromResult(new TemperatureResult(TemperatureStatus.Valid))
        ]);

        var result = cpu.Read();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Invalid, result.Status);
        Assert.Null(result.Reading);
    }

    [Fact]
    public async Task AsyncValidStatusWithoutReadingIsRejected()
    {
        using var cpu = new CoreCpuT([
            FakeTemperatureProvider.FromResult(new TemperatureResult(TemperatureStatus.Valid))
        ]);

        var result = await cpu.ReadAsync();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Invalid, result.Status);
        Assert.Null(result.Reading);
    }

    [Fact]
    public void NonValidStatusWithReadingRemainsNonValid()
    {
        var reading = new TemperatureReading(65.0, DateTimeOffset.UnixEpoch);
        using var cpu = new CoreCpuT([
            FakeTemperatureProvider.FromResult(new TemperatureResult(
                TemperatureStatus.Unavailable,
                reading))
        ]);

        var result = cpu.Read();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Unavailable, result.Status);
        Assert.Equal(reading, result.Reading);
    }

    [Fact]
    public void ReadingBelowMinimumIsRejected()
    {
        using var cpu = new CoreCpuT([FakeTemperatureProvider.Valid(-51.0)]);

        var result = cpu.Read();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Invalid, result.Status);
        Assert.Null(result.Reading);
    }

    [Fact]
    public void ReadingAboveMaximumIsRejected()
    {
        using var cpu = new CoreCpuT([FakeTemperatureProvider.Valid(151.0)]);

        var result = cpu.Read();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Invalid, result.Status);
        Assert.Null(result.Reading);
    }

    [Fact]
    public void MinimumBoundaryIsAccepted()
    {
        using var cpu = new CoreCpuT([FakeTemperatureProvider.Valid(-50.0)]);

        var result = cpu.Read();

        Assert.True(result.IsValid);
        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.NotNull(result.Reading);
        Assert.Equal(-50.0, result.Reading.Celsius);
    }

    [Fact]
    public void MaximumBoundaryIsAccepted()
    {
        using var cpu = new CoreCpuT([FakeTemperatureProvider.Valid(150.0)]);

        var result = cpu.Read();

        Assert.True(result.IsValid);
        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.NotNull(result.Reading);
        Assert.Equal(150.0, result.Reading.Celsius);
    }

    [Fact]
    public void NormalTemperatureIsAccepted()
    {
        using var cpu = new CoreCpuT([FakeTemperatureProvider.Valid(65.0)]);

        var result = cpu.Read();

        Assert.True(result.IsValid);
        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.NotNull(result.Reading);
        Assert.Equal(65.0, result.Reading.Celsius);
    }

    [Fact]
    public void InvalidReadingFallsBackToValidProvider()
    {
        var providerA = FakeTemperatureProvider.Valid(200.0);
        var providerB = FakeTemperatureProvider.Valid(65.0);
        using var cpu = new CoreCpuT([providerA, providerB]);

        var result = cpu.Read();

        Assert.True(result.IsValid);
        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.NotNull(result.Reading);
        Assert.Equal(65.0, result.Reading.Celsius);
        Assert.Equal(1, providerA.CallCount);
        Assert.Equal(1, providerB.CallCount);
    }

    [Fact]
    public void InvalidReadingsFromAllProvidersReturnNoSuccessfulReading()
    {
        var providerA = FakeTemperatureProvider.Valid(-100.0);
        var providerB = FakeTemperatureProvider.Valid(200.0);
        using var cpu = new CoreCpuT([providerA, providerB]);

        var result = cpu.Read();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Invalid, result.Status);
        Assert.Null(result.Reading);
        Assert.Equal(1, providerA.CallCount);
        Assert.Equal(1, providerB.CallCount);
    }
}