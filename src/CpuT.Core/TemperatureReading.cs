namespace CpuT.Core;

public sealed record TemperatureReading(
    double Celsius,
    DateTimeOffset Timestamp,
    string? SensorName = null);
