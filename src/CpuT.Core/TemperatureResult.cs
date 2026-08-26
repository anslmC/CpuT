namespace CpuT.Core;

public sealed record TemperatureResult(
    TemperatureStatus Status,
    TemperatureReading? Reading = null,
    string? Error = null)
{
    public bool IsValid => Status == TemperatureStatus.Valid && Reading is not null;

    public static TemperatureResult Valid(TemperatureReading reading) =>
        new(TemperatureStatus.Valid, reading);

    public static TemperatureResult Unavailable(string? error = null) =>
        new(TemperatureStatus.Unavailable, Error: error);

    public static TemperatureResult Unsupported(string? error = null) =>
        new(TemperatureStatus.Unsupported, Error: error);

    public static TemperatureResult Invalid(string? error = null) =>
        new(TemperatureStatus.Invalid, Error: error);

    public static TemperatureResult Failed(string? error = null) =>
        new(TemperatureStatus.Failed, Error: error);
}
