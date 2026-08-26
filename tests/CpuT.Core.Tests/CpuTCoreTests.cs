using CpuT.Core;
using CoreCpuT = CpuT.Core.CpuT;

namespace CpuT.Core.Tests;

public class CpuTCoreTests
{
    [Fact]
    public void EmptyProviderListIsUnsupported()
    {
        var result = new CoreCpuT([]).Read();

        Assert.Equal(TemperatureStatus.Unsupported, result.Status);
        Assert.Null(result.Reading);
    }
}
