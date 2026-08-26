namespace CpuT.Core;

internal static class TemperatureValidation
{
    private const double MinimumCelsius = -50;
    private const double MaximumCelsius = 150;

    public static TemperatureResult Validate(TemperatureResult result)
    {
        if (!result.IsValid || result.Reading is null)
        {
            return result;
        }

        var value = result.Reading.Celsius;
        return double.IsFinite(value) && value >= MinimumCelsius && value <= MaximumCelsius
            ? result
            : TemperatureResult.Invalid("The temperature was outside the plausible Celsius range.");
    }
}
