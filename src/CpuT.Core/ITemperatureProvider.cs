namespace CpuT.Core;

public interface ITemperatureProvider
{
    bool IsPlatformSupported();

    TemperatureResult TryRead();

    Task<TemperatureResult> TryReadAsync(CancellationToken cancellationToken = default);
}
