namespace CpuT.Core;

internal sealed class ProviderCache : IDisposable
{
    private const int FailureThreshold = 3;
    private readonly object sync = new();
    private readonly ProviderDiscovery discovery;
    private readonly SemaphoreSlim discoveryGate = new(1, 1);
    private readonly CooldownPolicy cooldown = new();
    private readonly IReadOnlyList<ITemperatureProvider> allProviders;
    private ITemperatureProvider? provider;
    private int consecutiveFailures;
    private bool disposed;

    public ProviderCache(IEnumerable<ITemperatureProvider> providers)
    {
        allProviders = providers.ToArray();
        discovery = new ProviderDiscovery(allProviders);
    }

    public TemperatureResult Read()
    {
        ThrowIfDisposed();

        var cachedProvider = GetProvider();
        if (cachedProvider is not null)
            return Record(cachedProvider, ReadProvider(cachedProvider));

        discoveryGate.Wait();
        try
        {
            return DiscoverSynchronously();
        }
        finally
        {
            discoveryGate.Release();
        }
    }

    public async Task<TemperatureResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var cachedProvider = GetProvider();
        if (cachedProvider is not null)
            return Record(cachedProvider, await ReadProviderAsync(cachedProvider, cancellationToken));

        await discoveryGate.WaitAsync(cancellationToken);
        try
        {
            return await DiscoverAsync(cancellationToken);
        }
        finally
        {
            discoveryGate.Release();
        }
    }

    private ITemperatureProvider? GetProvider()
    {
        lock (sync)
        {
            return provider;
        }
    }

    private TemperatureResult DiscoverSynchronously()
    {
        var existing = GetProvider();
        if (existing is not null)
            return Record(existing, ReadProvider(existing));

        if (IsCoolingDown())
            return CooldownResult();

        var discoveryResult = discovery.Discover();
        lock (sync)
        {
            UpdateDiscoveryState(discoveryResult.Provider);
        }
        return discoveryResult.Result;
    }

    private async Task<TemperatureResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        var existing = GetProvider();
        if (existing is not null)
            return Record(existing, await ReadProviderAsync(existing, cancellationToken));

        if (IsCoolingDown())
            return CooldownResult();

        var discoveryResult = await discovery.DiscoverAsync(cancellationToken);
        lock (sync)
        {
            UpdateDiscoveryState(discoveryResult.Provider);
        }

        return discoveryResult.Result;
    }

    private bool IsCoolingDown()
    {
        lock (sync)
        {
            return cooldown.IsActive(DateTimeOffset.UtcNow);
        }
    }

    private static TemperatureResult CooldownResult() =>
        TemperatureResult.Failed("Provider discovery is cooling down.", TemperatureFailureReason.Cooldown);

    private void UpdateDiscoveryState(ITemperatureProvider? discoveredProvider)
    {
        lock (sync)
        {
            if (discoveredProvider is not null)
            {
                provider = discoveredProvider;
                consecutiveFailures = 0;
                cooldown.Clear();
            }
            else
            {
                cooldown.Start(DateTimeOffset.UtcNow);
            }
        }
    }

    private static TemperatureResult ReadProvider(ITemperatureProvider provider)
    {
        try
        {
            return provider.TryRead();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ExceptionMapper.ToFailure(exception);
        }
    }

    private static async Task<TemperatureResult> ReadProviderAsync(
        ITemperatureProvider provider,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.TryReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ExceptionMapper.ToFailure(exception);
        }
    }

    private TemperatureResult Record(ITemperatureProvider cachedProvider, TemperatureResult rawResult)
    {
        var result = TemperatureValidation.Validate(rawResult);
        lock (sync)
        {
            if (provider != cachedProvider)
            {
                return result;
            }

            if (result.IsValid)
            {
                consecutiveFailures = 0;
            }
            else if (result.Status is TemperatureStatus.Failed or TemperatureStatus.Invalid)
            {
                consecutiveFailures++;
                if (consecutiveFailures >= FailureThreshold)
                {
                    provider = null;
                    consecutiveFailures = 0;
                    cooldown.Start(DateTimeOffset.UtcNow);
                }
            }
        }

        return result;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(ProviderCache));
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        discoveryGate.Dispose();

        foreach (var candidate in allProviders)
        {
            if (candidate is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}