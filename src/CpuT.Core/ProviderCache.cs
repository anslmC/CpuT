namespace CpuT.Core;

internal sealed class ProviderCache
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
        var cachedProvider = GetProvider();
        if (cachedProvider is null)
        {
            return DiscoverSynchronously();
        }

        return Record(cachedProvider, cachedProvider.TryRead());
    }

    public async Task<TemperatureResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cachedProvider = GetProvider();
        if (cachedProvider is null)
        {
            return await DiscoverAsync(cancellationToken);
        }

        return Record(cachedProvider, await cachedProvider.TryReadAsync(cancellationToken));
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
        discoveryGate.Wait();
        try
        {
            lock (sync)
            {
                if (provider is not null)
                {
                    return Record(provider, provider.TryRead());
                }

                if (cooldown.IsActive(DateTimeOffset.UtcNow))
                {
                    return TemperatureResult.Failed("Provider discovery is cooling down.");
                }
            }

            var discoveryResult = discovery.Discover();
            lock (sync)
            {
                if (discoveryResult.Provider is not null)
                {
                    provider = discoveryResult.Provider;
                    cooldown.Clear();
                }
                else
                {
                    cooldown.Start(DateTimeOffset.UtcNow);
                }
            }

            return discoveryResult.Result;
        }
        finally
        {
            discoveryGate.Release();
        }
    }

    private async Task<TemperatureResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        await discoveryGate.WaitAsync(cancellationToken);
        ITemperatureProvider? cachedProvider;
        try
        {
            lock (sync)
            {
                cachedProvider = provider;

                if (cachedProvider is null && cooldown.IsActive(DateTimeOffset.UtcNow))
                {
                    return TemperatureResult.Failed("Provider discovery is cooling down.");
                }
            }

            if (cachedProvider is not null)
            {
                return Record(cachedProvider, await cachedProvider.TryReadAsync(cancellationToken));
            }

            var discoveryResult = await discovery.DiscoverAsync(CancellationToken.None);
            lock (sync)
            {
                if (discoveryResult.Provider is not null)
                {
                    provider = discoveryResult.Provider;
                    cooldown.Clear();
                }
                else
                {
                    cooldown.Start(DateTimeOffset.UtcNow);
                }
            }

            return discoveryResult.Result;
        }
        finally
        {
            discoveryGate.Release();
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
}
