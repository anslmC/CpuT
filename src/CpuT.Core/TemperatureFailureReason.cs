namespace CpuT.Core;
 
public enum TemperatureFailureReason
{
    None,
    AccessDenied,
    ProviderError,
    Cooldown,
    Unknown
}