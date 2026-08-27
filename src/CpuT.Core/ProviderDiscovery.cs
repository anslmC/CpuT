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
        TemperatureResult? lastFailure = null;

        foreach (var provider in providers)
        {
            bool isSupported;
            try
            {
                isSupported = provider.IsPlatformSupported();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastFailure = Failure(exception);
                continue;
            }

            if (!isSupported)
            {
                continue;
            }

            var result = Read(provider);
            if (result.IsValid)
            {
                return (provider, result);
            }

            lastResult = result;
            if (result.Status == TemperatureStatus.Failed)
                lastFailure = result;
        }

        return (null, lastFailure ?? lastResult);
    }

    public async Task<(ITemperatureProvider? Provider, TemperatureResult Result)> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var lastResult = TemperatureResult.Unsupported("No supported temperature provider is available.");
        TemperatureResult? lastFailure = null;

        foreach (var provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool isSupported;
            try
            {
                isSupported = provider.IsPlatformSupported();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastFailure = Failure(exception);
                continue;
            }

            if (!isSupported)
            {
                continue;
            }

            var result = TemperatureValidation.Validate(await ReadAsync(provider, cancellationToken));
            if (result.IsValid)
            {
                return (provider, result);
            }

            lastResult = result;
            if (result.Status == TemperatureStatus.Failed)
                lastFailure = result;
        }

        return (null, lastFailure ?? lastResult);
    }

    private static TemperatureResult Read(ITemperatureProvider provider)
    {
        try
        {
            return TemperatureValidation.Validate(provider.TryRead());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Failure(exception);
        }
    }

    private static async Task<TemperatureResult> ReadAsync(
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
            return Failure(exception);
        }
    }

    private static TemperatureResult Failure(Exception exception) =>
        exception is UnauthorizedAccessException
            ? TemperatureResult.Failed("The temperature provider access was denied.", TemperatureFailureReason.AccessDenied)
            : TemperatureResult.Failed("The temperature provider encountered an error.", TemperatureFailureReason.ProviderError);
}
