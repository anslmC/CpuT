using CpuT;
using CpuT.Core;

namespace CpuT.Integration.Tests;

public class CompositionTests
{
    [Fact]
    public void CompositionReturnsAResultWithoutHardwareAccess()
    {
        var result = PlatformProviderComposition.Create().Read();

        Assert.Contains(result.Status, new[]
        {
            TemperatureStatus.Unsupported,
            TemperatureStatus.Unavailable,
            TemperatureStatus.Invalid,
            TemperatureStatus.Failed,
            TemperatureStatus.Valid
        });
    }
}
