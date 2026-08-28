using CpuT.Core;
using CoreCpuT = CpuT.Core.CpuT;

namespace CpuT.Core.Tests;

public class CpuTCoreTests
{
    [Fact]
    public void NullProviderCollectionThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CoreCpuT(null!));

        Assert.Equal("providers", exception.ParamName);
    }

    [Fact]
    public void EmptyProviderListIsUnsupported()
    {
        var result = new CoreCpuT([]).Read();

        Assert.Equal(TemperatureStatus.Unsupported, result.Status);
        Assert.Null(result.Reading);
    }

    [Fact]
    public void ProviderExceptionIsFailedAndFallbackContinues()
    {
        var fallback = new TestProvider(() => TemperatureResult.Valid(new TemperatureReading(42, DateTimeOffset.UtcNow)));
        var result = new CoreCpuT([
            new TestProvider(() => throw new InvalidOperationException()),
            fallback
        ]).Read();

        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.Equal(1, fallback.ReadCount);
    }

    [Fact]
    public void ProviderExceptionIncludesMachineReadableReason()
    {
        var result = new CoreCpuT([new TestProvider(() => throw new UnauthorizedAccessException())]).Read();

        Assert.Equal(TemperatureStatus.Failed, result.Status);
        Assert.Equal(TemperatureFailureReason.AccessDenied, result.FailureReason);
    }

    [Fact]
    public void ProviderFailureIsNotHiddenByUnsupportedFallback()
    {
        var result = new CoreCpuT([
            new TestProvider(() => throw new InvalidOperationException()),
            new TestProvider(() => TemperatureResult.Unsupported())
        ]).Read();

        Assert.Equal(TemperatureStatus.Failed, result.Status);
        Assert.Equal(TemperatureFailureReason.ProviderError, result.FailureReason);
    }

    [Fact]
    public async Task AsyncCancellationReachesProviderAndIsNotConvertedToFailure()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new TestProvider(
            () => TemperatureResult.Unsupported(),
            token =>
            {
                cancellation.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.FromResult(TemperatureResult.Valid(new TemperatureReading(40, DateTimeOffset.UtcNow)));
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new CoreCpuT([provider]).ReadAsync(cancellation.Token));
        Assert.True(provider.AsyncTokenObserved);
    }

    [Fact]
    public void CachedProviderIsReusedAndDiscoveryCooldownPersists()
    {
        var provider = new TestProvider(() => TemperatureResult.Valid(new TemperatureReading(40, DateTimeOffset.UtcNow)));
        using var cpu = new CoreCpuT([provider]);

        Assert.Equal(TemperatureStatus.Valid, cpu.Read().Status);
        Assert.Equal(TemperatureStatus.Valid, cpu.Read().Status);
        Assert.Equal(2, provider.ReadCount);

        var failingProvider = new TestProvider(() => TemperatureResult.Failed("failure"));
        using var failingCpu = new CoreCpuT([failingProvider]);
        Assert.Equal(TemperatureStatus.Failed, failingCpu.Read().Status);
        Assert.Equal(TemperatureStatus.Failed, failingCpu.Read().Status);
        Assert.Equal(1, failingProvider.ReadCount);
    }

    [Fact]
    public async Task ConcurrentReadsAreSerialized()
    {
        var activeReads = 0;
        var provider = new TestProvider(() =>
        {
            if (Interlocked.Increment(ref activeReads) != 1)
                throw new InvalidOperationException("Concurrent provider access.");

            Thread.Sleep(5);
            Interlocked.Decrement(ref activeReads);
            return TemperatureResult.Valid(new TemperatureReading(40, DateTimeOffset.UtcNow));
        });
        using var cpu = new CoreCpuT([provider]);

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(cpu.Read)));

        Assert.All(results, result => Assert.Equal(TemperatureStatus.Valid, result.Status));
    }

    [Fact]
    public void DisposalReleasesCacheResources()
    {
        var cpu = new CoreCpuT([]);
        cpu.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cpu.Read());
    }

    private sealed class TestProvider(
        Func<TemperatureResult> read,
        Func<CancellationToken, Task<TemperatureResult>>? asyncRead = null) : ITemperatureProvider
    {
        public int ReadCount { get; private set; }
        public bool AsyncTokenObserved { get; private set; }

        public bool IsPlatformSupported() => true;

        public TemperatureResult TryRead()
        {
            ReadCount++;
            return read();
        }

        public async Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default)
        {
            AsyncTokenObserved = cancellationToken.CanBeCanceled;
            return asyncRead is null ? TryRead() : await asyncRead(cancellationToken);
        }
    }
}
