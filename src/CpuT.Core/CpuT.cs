namespace CpuT.Core;

public sealed class CpuT
{
    private readonly ProviderCache cache;

    public CpuT(IEnumerable<ITemperatureProvider> providers)
    {
        cache = new ProviderCache(providers);
    }

    public TemperatureResult Read() => cache.Read();

    public Task<TemperatureResult> ReadAsync(CancellationToken cancellationToken = default) =>
        cache.ReadAsync(cancellationToken);
}
