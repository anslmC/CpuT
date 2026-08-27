using CpuT.Core;
using CpuTFacade = global::CpuT.CpuT;

var result = CpuTFacade.Read();
if (result.IsValid)
{
	Console.WriteLine($"Temp: {result.Reading!.Celsius:F1}°C");
	Console.WriteLine("Status: Success");
}
else
{
	Console.WriteLine($"Temp: {(result.Status == TemperatureStatus.Invalid ? "Invalid" : "Unavailable")}");
	Console.WriteLine($"Status: {(result.Status == TemperatureStatus.Invalid ? "Failed" : result.Status)}");

	if (!string.IsNullOrWhiteSpace(result.Error))
	{
		var reason = result.Status == TemperatureStatus.Failed
			? $"{result.FailureReason} - "
			: string.Empty;
		Console.WriteLine($"Reason: {reason}{result.Error}");
	}
}
