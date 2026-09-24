namespace StudyFlow.Tests.Architecture;

public sealed class LayerDependencyTests
{
    [Fact]
    public void DomainAssembly_ShouldNotReferenceOuterLayers()
    {
        var references = typeof(Domain.Common.BaseEntity).Assembly.GetReferencedAssemblies().Select(x => x.Name);
        Assert.DoesNotContain("StudyFlow.Application", references);
        Assert.DoesNotContain("StudyFlow.Infrastructure", references);
        Assert.DoesNotContain("StudyFlow.API", references);
    }
}
