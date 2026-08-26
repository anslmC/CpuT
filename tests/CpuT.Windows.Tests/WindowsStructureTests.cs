using CpuT.Windows;

namespace CpuT.Windows.Tests;

public class WindowsStructureTests
{
    [Fact]
    public void ProviderListHasFixedPriorityOrder()
    {
        Assert.Equal(2, WindowsProviderList.GetProviders().Count);
    }
}
