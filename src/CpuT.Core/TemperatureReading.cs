namespace CpuT.Core;

/// <summary>
/// Represents a single CPU temperature reading captured at a specific point in time.
/// </summary>
/// <remarks>
/// The value is expressed in degrees Celsius and is intended to represent a consumer-visible
/// temperature sample rather than an internal provider implementation detail.
/// </remarks>
public sealed record TemperatureReading(
    double Celsius,
    DateTimeOffset Timestamp,
    string? SensorName = null);
