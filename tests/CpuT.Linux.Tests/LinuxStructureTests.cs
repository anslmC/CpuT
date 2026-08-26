using CpuT.Linux;

namespace CpuT.Linux.Tests;

public class LinuxStructureTests
{
    [Fact]
    public void ProviderListHasFixedPriorityOrder()
    {
        Assert.Equal(2, LinuxProviderList.GetProviders().Count);
    }
}
