namespace CpuT.Core;

internal static class ExceptionMapper
{
    public static TemperatureResult ToFailure(Exception exception) => exception switch
    {
        UnauthorizedAccessException => TemperatureResult.Failed(
            "The temperature provider access was denied.",
            TemperatureFailureReason.AccessDenied),
        _ => TemperatureResult.Failed(
            "The temperature provider encountered an error.",
            TemperatureFailureReason.ProviderError)
    };
}