using CpuT.Core;
using CpuTFacade = global::CpuT.CpuT;

var result = CpuTFacade.Read();
Console.WriteLine($"Status: {result.Status}");
if (result.Status == TemperatureStatus.Valid)
{
	Console.WriteLine($"Temperature: {result.Reading!.Celsius:F1} C");
}
else if (result.Status == TemperatureStatus.Failed)
{
	Console.WriteLine($"Failure: {result.FailureReason} - {result.Error}");
}
