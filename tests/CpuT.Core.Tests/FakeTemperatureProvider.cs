using CpuT.Core;

namespace CpuT.Core.Tests;

// TEMPORARY PHASE 3 TEST INFRASTRUCTURE
// Used only to verify provider fallback behavior.
// Not part of the production API.
internal sealed class FakeTemperatureProvider : ITemperatureProvider
{
    private readonly TemperatureResult result;

    private FakeTemperatureProvider(TemperatureResult result)
    {
        this.result = result;
    }

    public int CallCount { get; private set; }

    public static FakeTemperatureProvider Unsupported(string? error = null) =>
        new(TemperatureResult.Unsupported(error));

    public static FakeTemperatureProvider Failed(
        string? error = "Temporary fake provider failure.",
        TemperatureFailureReason failureReason = TemperatureFailureReason.ProviderError) =>
        new(TemperatureResult.Failed(error, failureReason));

    public static FakeTemperatureProvider FromResult(TemperatureResult result) => new(result);

    public static FakeTemperatureProvider Valid(double celsius) =>
        new(TemperatureResult.Valid(new TemperatureReading(
            celsius,
            DateTimeOffset.UnixEpoch,
            "Temporary fake sensor")));

    public bool IsPlatformSupported() => true;

    public TemperatureResult TryRead()
    {
        CallCount++;
        return result;
    }

    public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TryRead());
}