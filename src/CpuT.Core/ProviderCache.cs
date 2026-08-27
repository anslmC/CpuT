namespace CpuT.Core;

internal sealed class ProviderCache : IDisposable
{
    private const int FailureThreshold = 3;
    private readonly object sync = new();
    private readonly ProviderDiscovery discovery;
    private readonly SemaphoreSlim discoveryGate = new(1, 1);
    private readonly CooldownPolicy cooldown = new();
    private ITemperatureProvider? provider;
    private int consecutiveFailures;

    public ProviderCache(IEnumerable<ITemperatureProvider> providers)
    {
        discovery = new ProviderDiscovery(providers);
    }

    public TemperatureResult Read()
    {
        discoveryGate.Wait();
        try
        {
            var cachedProvider = GetProvider();
            return cachedProvider is null
                ? DiscoverSynchronously()
                : Record(cachedProvider, ReadProvider(cachedProvider));
        }
        finally
        {
            discoveryGate.Release();
        }
    }

    public async Task<TemperatureResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await discoveryGate.WaitAsync(cancellationToken);
        try
        {
            var cachedProvider = GetProvider();
            return cachedProvider is null
                ? await DiscoverAsync(cancellationToken)
                : Record(cachedProvider, await ReadProviderAsync(cachedProvider, cancellationToken));
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
        lock (sync)
        {
            if (provider is not null)
            {
                return Record(provider, ReadProvider(provider));
            }

            if (cooldown.IsActive(DateTimeOffset.UtcNow))
            {
                return TemperatureResult.Failed("Provider discovery is cooling down.");
            }

            var discoveryResult = discovery.Discover();
            UpdateDiscoveryState(discoveryResult.Provider);
            return discoveryResult.Result;
        }
    }

    private async Task<TemperatureResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        ITemperatureProvider? cachedProvider;
        lock (sync)
        {
            cachedProvider = provider;
            if (cachedProvider is null && cooldown.IsActive(DateTimeOffset.UtcNow))
                return TemperatureResult.Failed("Provider discovery is cooling down.");
        }

        if (cachedProvider is not null)
            return Record(cachedProvider, await ReadProviderAsync(cachedProvider, cancellationToken));

        var discoveryResult = await discovery.DiscoverAsync(cancellationToken);
        lock (sync)
        {
            UpdateDiscoveryState(discoveryResult.Provider);
        }

        return discoveryResult.Result;
    }

    private void UpdateDiscoveryState(ITemperatureProvider? discoveredProvider)
    {
        if (discoveredProvider is not null)
        {
            provider = discoveredProvider;
            cooldown.Clear();
        }
        else
        {
            cooldown.Start(DateTimeOffset.UtcNow);
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
        catch (UnauthorizedAccessException)
        {
            return TemperatureResult.Failed("The temperature provider access was denied.", TemperatureFailureReason.AccessDenied);
        }
        catch (Exception)
        {
            return TemperatureResult.Failed("The temperature provider encountered an error.", TemperatureFailureReason.ProviderError);
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
        catch (UnauthorizedAccessException)
        {
            return TemperatureResult.Failed("The temperature provider access was denied.", TemperatureFailureReason.AccessDenied);
        }
        catch (Exception)
        {
            return TemperatureResult.Failed("The temperature provider encountered an error.", TemperatureFailureReason.ProviderError);
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
                }
            }
        }

        return result;
    }

    public void Dispose() => discoveryGate.Dispose();
}
