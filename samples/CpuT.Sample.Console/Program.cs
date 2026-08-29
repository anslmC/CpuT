using CpuT.Core;
using CpuTFacade = global::CpuT.CpuT;

var result = CpuTFacade.Read();

// Display the result, clearly distinguishing all states
switch (result.Status)
{
	case TemperatureStatus.Valid:
		Console.WriteLine($"Temperature: {result.Reading!.Celsius:F1}°C");
		break;

	case TemperatureStatus.Unavailable:
		Console.WriteLine("Temperature reading unavailable at this time.");
		break;

	case TemperatureStatus.Unsupported:
		Console.WriteLine("This environment does not support CPU temperature readings.");
		break;

	case TemperatureStatus.Invalid:
		Console.WriteLine("Temperature reading was rejected as invalid.");
		break;

	case TemperatureStatus.Failed:
		Console.WriteLine("Temperature read failed.");
		if (result.FailureReason != TemperatureFailureReason.None)
		{
			Console.WriteLine($"Failure reason: {result.FailureReason}");
		}
		break;
}

// Display error details if available
if (!string.IsNullOrWhiteSpace(result.Error))
{
	Console.WriteLine($"Details: {result.Error}");
}
