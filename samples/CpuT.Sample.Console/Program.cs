using CpuTFacade = global::CpuT.CpuT;

var result = CpuTFacade.Read();
Console.WriteLine($"Status: {result.Status}");
