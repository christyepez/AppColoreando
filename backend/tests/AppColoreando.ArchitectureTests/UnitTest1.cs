using NetArchTest.Rules;

namespace AppColoreando.ArchitectureTests;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_must_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(typeof(AppColoreando.Domain.Entities.AppUser).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "AppColoreando.Application",
                "AppColoreando.Infrastructure",
                "AppColoreando.Api",
                "Microsoft.EntityFrameworkCore")
            .GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_must_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(typeof(AppColoreando.Application.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOnAny("AppColoreando.Infrastructure", "AppColoreando.Api")
            .GetResult();
        Assert.True(result.IsSuccessful);
    }
}
