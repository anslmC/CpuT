namespace CpuT.Core;
 
public sealed class CpuT : IDisposable
{
    private readonly ProviderCache cache;
 
    public CpuT(IEnumerable<ITemperatureProvider> providers)
    {
        cache = new ProviderCache(providers);
    }
 
    public TemperatureResult Read() => cache.Read();
 
    public Task<TemperatureResult> ReadAsync(CancellationToken cancellationToken = default) =>
        cache.ReadAsync(cancellationToken);
 
    /// <summary>
    /// Releases the providers owned by this instance.
    /// </summary>
    /// <exception cref="AggregateException">
    /// Thrown when one or more providers fail to dispose cleanly.
    /// </exception>
    public void Dispose() => cache.Dispose();
}