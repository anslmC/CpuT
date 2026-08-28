namespace CpuT.Core;
 
public sealed class CpuT : IDisposable
{
    private readonly ProviderCache cache;
 
    /// <summary>
    /// Creates a CPU temperature monitor using the supplied providers.
    /// </summary>
    /// <remarks>
    /// This instance takes ownership of the supplied providers and disposes each
    /// disposable provider when it is disposed. Do not reuse or share those
    /// provider instances with other owners after passing them here.
    /// </remarks>
    /// <param name="providers">The providers to use for temperature discovery and reads.</param>
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
    /// <remarks>
    /// Disposal waits for all in-flight reads to complete and has no timeout, so
    /// a provider read that never returns can block shutdown indefinitely. Do not
    /// call Dispose from within a provider's TryRead or TryReadAsync implementation,
    /// because that can deadlock while disposal waits for the active read to finish.
    /// </remarks>
    /// <exception cref="AggregateException">
    /// Thrown when one or more providers fail to dispose cleanly.
    /// </exception>
    public void Dispose() => cache.Dispose();
}