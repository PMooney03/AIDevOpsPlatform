using DevOps.Core.Entities;

namespace DevOps.UnitTests.Architecture;

public class CoreIndependenceTests
{
    [Fact]
    public void Core_does_not_reference_infrastructure_or_web_frameworks()
    {
        var referenced = typeof(MonitoredService).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name!)
            .ToArray();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, name => name.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, name => name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, name => name.Contains("Swashbuckle", StringComparison.OrdinalIgnoreCase));
    }
}
