using CpuT.Core;
using CoreCpuT = CpuT.Core.CpuT;

namespace CpuT.Core.Tests;

public class ProviderCacheLifecycleTests
{
    [Fact]
    public async Task DisposeWaitsForActiveSynchronousRead()
    {
        using var provider = new BlockingProvider();
        using var cpu = new CoreCpuT([provider]);

        var read = Task.Run(cpu.Read);
        Assert.True(provider.SyncReadStarted.Wait(TimeSpan.FromSeconds(5)));

        var dispose = Task.Run(cpu.Dispose);
        Assert.NotSame(dispose, await Task.WhenAny(dispose, Task.Delay(100)));
        Assert.False(provider.IsDisposed);

        provider.ReleaseRead();

        Assert.Equal(TemperatureStatus.Valid, (await read).Status);
        await dispose;
        Assert.True(provider.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => cpu.Read());
    }

    [Fact]
    public async Task DisposeWaitsForActiveAsynchronousRead()
    {
        using var provider = new BlockingProvider();
        using var cpu = new CoreCpuT([provider]);

        var read = cpu.ReadAsync();
        Assert.True(provider.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

        var dispose = Task.Run(cpu.Dispose);
        Assert.NotSame(dispose, await Task.WhenAny(dispose, Task.Delay(100)));
        Assert.False(provider.IsDisposed);

        provider.ReleaseRead();

        Assert.Equal(TemperatureStatus.Valid, (await read).Status);
        await dispose;
        Assert.True(provider.IsDisposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => cpu.ReadAsync());
    }

    [Fact]
    public async Task MixedReadsRemainSerialized()
    {
        using var provider = new BlockingProvider();
        using var cpu = new CoreCpuT([provider]);

        var asyncRead = cpu.ReadAsync();
        Assert.True(provider.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

        var syncRead = Task.Run(cpu.Read);
        Assert.False(provider.SyncReadStarted.Wait(TimeSpan.FromMilliseconds(100)));

        provider.ReleaseRead();

        Assert.Equal(TemperatureStatus.Valid, (await asyncRead).Status);
        Assert.Equal(TemperatureStatus.Valid, (await syncRead).Status);
    }

    [Fact]
    public async Task ConcurrentDisposalIsIdempotentAndRejectsNewReads()
    {
        using var provider = new BlockingProvider();
        using var cpu = new CoreCpuT([provider]);

        var read = cpu.ReadAsync();
        Assert.True(provider.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

        var disposals = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(cpu.Dispose))
            .ToArray();

        var allDisposals = Task.WhenAll(disposals);
        Assert.NotSame(allDisposals, await Task.WhenAny(allDisposals, Task.Delay(100)));
        Assert.False(provider.IsDisposed);

        provider.ReleaseRead();

        Assert.Equal(TemperatureStatus.Valid, (await read).Status);
        await Task.WhenAll(disposals);
        Assert.Equal(1, provider.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => cpu.Read());
    }

    [Fact]
    public void ProviderDisposalContinuesAfterAnException()
    {
        var failingProvider = new DisposableProvider(throwsOnDispose: true);
        var laterProvider = new DisposableProvider();
        var cpu = new CoreCpuT([failingProvider, laterProvider]);

        Assert.Throws<AggregateException>(() => cpu.Dispose());

        Assert.Equal(1, failingProvider.DisposeCount);
        Assert.Equal(1, laterProvider.DisposeCount);
    }

    [Fact]
    public void AllProviderDisposalExceptionsAreAggregated()
    {
        var firstProvider = new DisposableProvider(throwsOnDispose: true);
        var secondProvider = new DisposableProvider(throwsOnDispose: true);
        var cpu = new CoreCpuT([firstProvider, secondProvider]);

        var exception = Assert.Throws<AggregateException>(() => cpu.Dispose());

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Equal(1, firstProvider.DisposeCount);
        Assert.Equal(1, secondProvider.DisposeCount);
    }

    [Fact]
    public void DuplicateProviderInstanceIsDisposedOnce()
    {
        var provider = new DisposableProvider();
        var cpu = new CoreCpuT([provider, provider]);

        cpu.Dispose();

        Assert.Equal(1, provider.DisposeCount);
    }

    [Fact]
    public async Task ConcurrentDisposalsAllObserveAggregatedException()
    {
        var firstProvider = new DisposableProvider(throwsOnDispose: true);
        var secondProvider = new DisposableProvider(throwsOnDispose: true);
        var cpu = new CoreCpuT([firstProvider, secondProvider]);

        var exceptions = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => Record.Exception(cpu.Dispose))));

        Assert.All(exceptions, exception =>
        {
            var aggregate = Assert.IsType<AggregateException>(exception);
            Assert.Equal(2, aggregate.InnerExceptions.Count);
        });
        Assert.Equal(1, firstProvider.DisposeCount);
        Assert.Equal(1, secondProvider.DisposeCount);
    }

    [Fact]
    public void DisposalWithoutProviderFailuresCompletesNormally()
    {
        var provider = new DisposableProvider();
        var cpu = new CoreCpuT([provider]);

        cpu.Dispose();

        Assert.Equal(1, provider.DisposeCount);
    }

    private sealed class BlockingProvider : ITemperatureProvider, IDisposable
    {
        private readonly ManualResetEventSlim syncRelease = new();
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ManualResetEventSlim ReadStarted { get; } = new();
        public ManualResetEventSlim SyncReadStarted { get; } = new();
        public bool IsDisposed { get; private set; }
        public int DisposeCount { get; private set; }

        public bool IsPlatformSupported() => true;

        public TemperatureResult TryRead()
        {
            SyncReadStarted.Set();
            syncRelease.Wait();
            return ValidResult();
        }

        public async Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default)
        {
            ReadStarted.Set();
            await release.Task.WaitAsync(cancellationToken);
            return ValidResult();
        }

        public void ReleaseRead()
        {
            syncRelease.Set();
            release.TrySetResult();
        }

        public void Dispose()
        {
            DisposeCount++;
            IsDisposed = true;
            syncRelease.Dispose();
        }

        private static TemperatureResult ValidResult() =>
            TemperatureResult.Valid(new TemperatureReading(40, DateTimeOffset.UtcNow));
    }

    private sealed class DisposableProvider(bool throwsOnDispose = false) : ITemperatureProvider, IDisposable
    {
        public int DisposeCount { get; private set; }

        public bool IsPlatformSupported() => true;

        public TemperatureResult TryRead() =>
            TemperatureResult.Valid(new TemperatureReading(40, DateTimeOffset.UtcNow));

        public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(TryRead());

        public void Dispose()
        {
            DisposeCount++;
            if (throwsOnDispose)
                throw new InvalidOperationException("Provider disposal failed.");
        }
    }
}