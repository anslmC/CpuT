namespace CpuT.Core;

/// <summary>
/// Provides on-demand CPU temperature reads using a set of provider instances.
/// </summary>
/// <remarks>
/// This instance owns the providers supplied to it and disposes any disposable providers when disposed.
/// It is intended for on-demand reads rather than continuous background monitoring.
/// </remarks>
public sealed class CpuT : IDisposable
{
    private readonly ProviderCache cache;

    /// <summary>
    /// Initializes a new monitor instance using the supplied providers.
    /// </summary>
    /// <param name="providers">
    /// The temperature providers to use for provider selection, fallback, and read operations.
    /// </param>
    /// <remarks>
    /// This instance takes ownership of the supplied providers and will dispose each disposable provider
    /// when the monitor is disposed. Do not reuse or share those provider instances with another owner
    /// after passing them here.
    /// </remarks>
    public CpuT(IEnumerable<ITemperatureProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        cache = new ProviderCache(providers);
    }

    /// <summary>
    /// Reads the current CPU temperature synchronously.
    /// </summary>
    /// <returns>
    /// The current result, which may indicate success, unavailability, unsupported environment,
    /// invalid data, or a provider failure.
    /// </returns>
    public TemperatureResult Read() => cache.Read();

    /// <summary>
    /// Reads the current CPU temperature asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the read before or during execution.
    /// </param>
    /// <returns>
    /// A task that resolves to the result of the read.
    /// </returns>
    public Task<TemperatureResult> ReadAsync(CancellationToken cancellationToken = default) =>
        cache.ReadAsync(cancellationToken);

    /// <summary>
    /// Releases the resources and providers owned by this instance.
    /// </summary>
    /// <remarks>
    /// Disposal waits for all in-flight reads to complete and has no timeout. A provider read that never
    /// returns can therefore block shutdown indefinitely. Do not call <see cref="Dispose"/> from within
    /// a provider's <see cref="ITemperatureProvider.TryRead"/> or <see cref="ITemperatureProvider.TryReadAsync"/>
    /// implementation, because that can deadlock while disposal waits for the active read to finish.
    /// </remarks>
    /// <exception cref="AggregateException">
    /// Thrown when one or more providers fail to dispose cleanly.
    /// </exception>
    public void Dispose() => cache.Dispose();
}