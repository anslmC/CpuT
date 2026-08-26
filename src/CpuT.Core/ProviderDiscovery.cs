namespace CpuT.Core;

internal sealed class ProviderDiscovery
{
    private readonly IReadOnlyList<ITemperatureProvider> providers;

    public ProviderDiscovery(IEnumerable<ITemperatureProvider> providers)
    {
        this.providers = providers.ToArray();
    }

    public (ITemperatureProvider? Provider, TemperatureResult Result) Discover()
    {
        var lastResult = TemperatureResult.Unsupported("No supported temperature provider is available.");

        foreach (var provider in providers)
        {
            if (!provider.IsPlatformSupported())
            {
                continue;
            }

            var result = TemperatureValidation.Validate(provider.TryRead());
            if (result.IsValid)
            {
                return (provider, result);
            }

            lastResult = result;
        }

        return (null, lastResult);
    }

    public async Task<(ITemperatureProvider? Provider, TemperatureResult Result)> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var lastResult = TemperatureResult.Unsupported("No supported temperature provider is available.");

        foreach (var provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!provider.IsPlatformSupported())
            {
                continue;
            }

            var result = TemperatureValidation.Validate(await provider.TryReadAsync(cancellationToken));
            if (result.IsValid)
            {
                return (provider, result);
            }

            lastResult = result;
        }

        return (null, lastResult);
    }
}
