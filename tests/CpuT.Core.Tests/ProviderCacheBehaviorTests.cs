using CpuT.Core;
using CoreCpuT = CpuT.Core.CpuT;

namespace CpuT.Core.Tests;

public class ProviderCacheBehaviorTests
{
    [Fact]
    public async Task CooldownExpiresAndRediscoveryIsAttemptedAgain()
    {
        var provider = new ScriptedProvider(
            TemperatureResult.Failed("initial failure"),
            TemperatureResult.Valid(new TemperatureReading(55, DateTimeOffset.UtcNow)));
        using var cpu = new CoreCpuT([provider]);

        Assert.Equal(TemperatureStatus.Failed, cpu.Read().Status);
        Assert.Equal(TemperatureFailureReason.Cooldown, cpu.Read().FailureReason);
        Assert.Equal(1, provider.SyncReadCount);

        await Task.Delay(TimeSpan.FromSeconds(2.1));

        var result = cpu.Read();

        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.Equal(2, provider.SyncReadCount);
    }

    [Fact]
    public async Task ThreeCachedFailuresEvictProviderAndRediscoverAfterCooldown()
    {
        var provider = new ScriptedProvider(
            TemperatureResult.Valid(new TemperatureReading(55, DateTimeOffset.UtcNow)),
            TemperatureResult.Failed("failure 1"),
            TemperatureResult.Invalid("failure 2"),
            TemperatureResult.Failed("failure 3"),
            TemperatureResult.Valid(new TemperatureReading(56, DateTimeOffset.UtcNow)));
        using var cpu = new CoreCpuT([provider]);

        Assert.Equal(TemperatureStatus.Valid, cpu.Read().Status);
        Assert.Equal(TemperatureStatus.Failed, cpu.Read().Status);
        Assert.Equal(TemperatureStatus.Invalid, cpu.Read().Status);
        Assert.Equal(TemperatureStatus.Failed, cpu.Read().Status);
        Assert.Equal(TemperatureFailureReason.Cooldown, cpu.Read().FailureReason);
        Assert.Equal(4, provider.SyncReadCount);

        await Task.Delay(TimeSpan.FromSeconds(2.1));

        Assert.Equal(TemperatureStatus.Valid, cpu.Read().Status);
        Assert.Equal(5, provider.SyncReadCount);
    }

    [Fact]
    public void ValidResultResetsConsecutiveFailureCounter()
    {
        var provider = new ScriptedProvider(
            TemperatureResult.Valid(new TemperatureReading(55, DateTimeOffset.UtcNow)),
            TemperatureResult.Failed("failure 1"),
            TemperatureResult.Invalid("failure 2"),
            TemperatureResult.Valid(new TemperatureReading(56, DateTimeOffset.UtcNow)),
            TemperatureResult.Failed("failure 3"),
            TemperatureResult.Failed("failure 4"),
            TemperatureResult.Valid(new TemperatureReading(57, DateTimeOffset.UtcNow)));
        using var cpu = new CoreCpuT([provider]);

        var results = Enumerable.Range(0, 7).Select(_ => cpu.Read()).ToArray();

        Assert.Equal(
            [
                TemperatureStatus.Valid,
                TemperatureStatus.Failed,
                TemperatureStatus.Invalid,
                TemperatureStatus.Valid,
                TemperatureStatus.Failed,
                TemperatureStatus.Failed,
                TemperatureStatus.Valid
            ],
            results.Select(result => result.Status));
        Assert.Equal(7, provider.SyncReadCount);
    }

    [Fact]
    public void UnavailableResultsDoNotTriggerEviction()
    {
        var provider = new ScriptedProvider(
            TemperatureResult.Valid(new TemperatureReading(55, DateTimeOffset.UtcNow)),
            TemperatureResult.Unavailable(),
            TemperatureResult.Unavailable(),
            TemperatureResult.Unavailable(),
            TemperatureResult.Valid(new TemperatureReading(56, DateTimeOffset.UtcNow)));
        using var cpu = new CoreCpuT([provider]);

        var results = Enumerable.Range(0, 5).Select(_ => cpu.Read()).ToArray();

        Assert.Equal(
            [
                TemperatureStatus.Valid,
                TemperatureStatus.Unavailable,
                TemperatureStatus.Unavailable,
                TemperatureStatus.Unavailable,
                TemperatureStatus.Valid
            ],
            results.Select(result => result.Status));
        Assert.Equal(5, provider.SyncReadCount);
    }

    [Fact]
    public async Task ConcurrentAsyncReadsReturnConsistentResults()
    {
        var provider = new ScriptedProvider(
            asyncRead: _ => Task.FromResult(
                TemperatureResult.Valid(new TemperatureReading(65, DateTimeOffset.UtcNow))));
        using var cpu = new CoreCpuT([provider]);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => cpu.ReadAsync()));

        Assert.All(results, result =>
        {
            Assert.Equal(TemperatureStatus.Valid, result.Status);
            Assert.Equal(65, result.Reading!.Celsius);
        });
        Assert.Equal(16, provider.AsyncReadCount);
    }

    [Fact]
    public async Task CancellationWhileQueuedDoesNotAffectInProgressRead()
    {
        var provider = new BlockingAsyncProvider();
        using var cpu = new CoreCpuT([provider]);

        var activeRead = cpu.ReadAsync();
        Assert.True(provider.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

        using var cancellation = new CancellationTokenSource();
        var queuedRead = cpu.ReadAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queuedRead);
        provider.ReleaseRead();

        Assert.Equal(TemperatureStatus.Valid, (await activeRead).Status);
        Assert.Equal(1, provider.ReadCount);
    }

    [Fact]
    public async Task CancellationBetweenProvidersStopsFurtherDiscovery()
    {
        using var cancellation = new CancellationTokenSource();
        var firstProvider = new ScriptedProvider(
            asyncRead: _ =>
            {
                cancellation.Cancel();
                return Task.FromResult(TemperatureResult.Unsupported());
            });
        var secondProvider = new ScriptedProvider(
            asyncRead: _ => Task.FromResult(
                TemperatureResult.Valid(new TemperatureReading(65, DateTimeOffset.UtcNow))));
        using var cpu = new CoreCpuT([firstProvider, secondProvider]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cpu.ReadAsync(cancellation.Token));

        Assert.Equal(1, firstProvider.AsyncReadCount);
        Assert.Equal(0, secondProvider.AsyncReadCount);
    }

    [Fact]
    public async Task AsyncProviderExceptionIsConvertedAndFallbackContinues()
    {
        var failingProvider = new ScriptedProvider(
            asyncRead: _ => Task.FromException<TemperatureResult>(
                new InvalidOperationException("async failure")));
        var fallbackProvider = new ScriptedProvider(
            asyncRead: _ => Task.FromResult(
                TemperatureResult.Valid(new TemperatureReading(65, DateTimeOffset.UtcNow))));
        using var cpu = new CoreCpuT([failingProvider, fallbackProvider]);

        var result = await cpu.ReadAsync();

        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.Equal(1, failingProvider.AsyncReadCount);
        Assert.Equal(1, fallbackProvider.AsyncReadCount);
    }

    [Fact]
    public async Task AsyncUnavailableProviderFallsBackInPriorityOrder()
    {
        var unavailableProvider = new ScriptedProvider(
            asyncRead: _ => Task.FromResult(TemperatureResult.Unavailable()));
        var fallbackProvider = new ScriptedProvider(
            asyncRead: _ => Task.FromResult(
                TemperatureResult.Valid(new TemperatureReading(65, DateTimeOffset.UtcNow))));
        using var cpu = new CoreCpuT([unavailableProvider, fallbackProvider]);

        var result = await cpu.ReadAsync();

        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.Equal(1, unavailableProvider.AsyncReadCount);
        Assert.Equal(1, fallbackProvider.AsyncReadCount);
    }

    [Fact]
    public void UnsupportedProviderIsSkippedWithoutReading()
    {
        var unsupportedProvider = new ScriptedProvider(isSupported: false);
        var fallbackProvider = new ScriptedProvider(
            TemperatureResult.Valid(new TemperatureReading(65, DateTimeOffset.UtcNow)));
        using var cpu = new CoreCpuT([unsupportedProvider, fallbackProvider]);

        var result = cpu.Read();

        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.Equal(0, unsupportedProvider.SyncReadCount);
        Assert.Equal(1, fallbackProvider.SyncReadCount);
    }

    [Fact]
    public void SupportCheckExceptionSkipsProvider()
    {
        var throwingProvider = new ScriptedProvider(
            supportCheck: () => throw new InvalidOperationException("support check failure"));
        var fallbackProvider = new ScriptedProvider(
            TemperatureResult.Valid(new TemperatureReading(65, DateTimeOffset.UtcNow)));
        using var cpu = new CoreCpuT([throwingProvider, fallbackProvider]);

        var result = cpu.Read();

        Assert.Equal(TemperatureStatus.Valid, result.Status);
        Assert.Equal(0, throwingProvider.SyncReadCount);
        Assert.Equal(1, fallbackProvider.SyncReadCount);
    }

    [Theory]
    [MemberData(nameof(StatusAndReadingCases))]
    public void StatusAndReadingCombinationsNormalizeThroughProviderPipeline(
        TemperatureStatus status,
        TemperatureReading? reading,
        TemperatureStatus expectedStatus,
        bool expectedValid)
    {
        using var cpu = new CoreCpuT([
            new ScriptedProvider(new TemperatureResult(status, reading))
        ]);

        var result = cpu.Read();

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedValid, result.IsValid);
    }

    public static TheoryData<TemperatureStatus, TemperatureReading?, TemperatureStatus, bool> StatusAndReadingCases =>
        new()
        {
            { TemperatureStatus.Valid, null, TemperatureStatus.Invalid, false },
            { TemperatureStatus.Valid, Reading(double.NaN), TemperatureStatus.Invalid, false },
            { TemperatureStatus.Valid, Reading(double.PositiveInfinity), TemperatureStatus.Invalid, false },
            { TemperatureStatus.Valid, Reading(double.NegativeInfinity), TemperatureStatus.Invalid, false },
            { TemperatureStatus.Valid, Reading(-50), TemperatureStatus.Valid, true },
            { TemperatureStatus.Valid, Reading(150), TemperatureStatus.Valid, true },
            { TemperatureStatus.Valid, Reading(65), TemperatureStatus.Valid, true },
            { TemperatureStatus.Unavailable, null, TemperatureStatus.Unavailable, false },
            { TemperatureStatus.Unavailable, Reading(65), TemperatureStatus.Unavailable, false },
            { TemperatureStatus.Unsupported, null, TemperatureStatus.Unsupported, false },
            { TemperatureStatus.Invalid, Reading(65), TemperatureStatus.Invalid, false },
            { TemperatureStatus.Failed, null, TemperatureStatus.Failed, false }
        };

    private static TemperatureReading Reading(double celsius) =>
        new(celsius, DateTimeOffset.UnixEpoch);

    private sealed class ScriptedProvider : ITemperatureProvider
    {
        private readonly Queue<TemperatureResult> results;
        private readonly Func<CancellationToken, Task<TemperatureResult>>? asyncRead;
        private readonly Func<bool>? supportCheck;
        private readonly bool isSupported;

        public ScriptedProvider(
            params TemperatureResult[] results)
            : this(true, null, null, results)
        {
        }

        public ScriptedProvider(
            Func<CancellationToken, Task<TemperatureResult>> asyncRead,
            bool isSupported = true)
            : this(isSupported, null, asyncRead, [])
        {
        }

        public ScriptedProvider(
            bool isSupported = true,
            Func<bool>? supportCheck = null,
            Func<CancellationToken, Task<TemperatureResult>>? asyncRead = null,
            params TemperatureResult[] results)
        {
            this.isSupported = isSupported;
            this.supportCheck = supportCheck;
            this.asyncRead = asyncRead;
            this.results = new Queue<TemperatureResult>(results);
        }

        public int SyncReadCount { get; private set; }
        public int AsyncReadCount { get; private set; }

        public bool IsPlatformSupported()
        {
            if (supportCheck is not null)
                return supportCheck();

            return isSupported;
        }

        public TemperatureResult TryRead()
        {
            SyncReadCount++;
            return results.Count > 0
                ? results.Dequeue()
                : TemperatureResult.Unsupported();
        }

        public Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default)
        {
            AsyncReadCount++;
            if (asyncRead is not null)
                return asyncRead(cancellationToken);

            return Task.FromResult(TryRead());
        }
    }

    private sealed class BlockingAsyncProvider : ITemperatureProvider
    {
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ManualResetEventSlim ReadStarted { get; } = new();
        public int ReadCount { get; private set; }

        public bool IsPlatformSupported() => true;

        public TemperatureResult TryRead() =>
            TemperatureResult.Valid(Reading(65));

        public async Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            ReadStarted.Set();
            await release.Task;
            return TemperatureResult.Valid(Reading(65));
        }

        public void ReleaseRead() => release.TrySetResult();
    }
}
