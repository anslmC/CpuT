using CpuT.Core;
using CoreCpuT = CpuT.Core.CpuT;

namespace CpuT.Core.Tests;

// TEMPORARY PHASE 3 TEST INFRASTRUCTURE
// Used only to verify provider fallback behavior.
// Not part of the production API.
public class ProviderFallbackTests
{
    [Fact]
    public void UnsupportedProviderFallsBackToValidProvider()
    {
        var providerA = FakeTemperatureProvider.Unsupported();
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
    public void FailedProviderFallsBackToValidProvider()
    {
        var providerA = FakeTemperatureProvider.Failed("Provider A failed.");
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
    public void AllUnsupportedProvidersReturnNoSuccessfulReading()
    {
        var providerA = FakeTemperatureProvider.Unsupported();
        var providerB = FakeTemperatureProvider.Unsupported();

        using var cpu = new CoreCpuT([providerA, providerB]);
        var result = cpu.Read();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Unsupported, result.Status);
        Assert.Null(result.Reading);
        Assert.Equal(1, providerA.CallCount);
        Assert.Equal(1, providerB.CallCount);
    }

    [Fact]
    public void AllFailedProvidersPreserveFinalFailureBehavior()
    {
        var providerA = FakeTemperatureProvider.Failed("Provider A failed.");
        var providerB = FakeTemperatureProvider.Failed("Provider B failed.");

        using var cpu = new CoreCpuT([providerA, providerB]);
        var result = cpu.Read();

        Assert.False(result.IsValid);
        Assert.Equal(TemperatureStatus.Failed, result.Status);
        Assert.Equal(TemperatureFailureReason.ProviderError, result.FailureReason);
        Assert.Null(result.Reading);
        Assert.Equal(1, providerA.CallCount);
        Assert.Equal(1, providerB.CallCount);
    }
}