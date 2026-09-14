using NetArchTest.Rules;
using System.Reflection;

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
            .ShouldNot().HaveDependencyOnAny("AppColoreando.Infrastructure", "AppColoreando.Api", "Microsoft.EntityFrameworkCore")
            .GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Controllers_must_not_inject_dbcontext_or_repositories()
    {
        var forbidden = typeof(Program).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .SelectMany(t => t.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            .SelectMany(c => c.GetParameters())
            .Where(p => p.ParameterType.Name.EndsWith("Repository", StringComparison.Ordinal) || p.ParameterType.Name.EndsWith("DbContext", StringComparison.Ordinal))
            .Select(p => $"{p.Member.DeclaringType?.Name}.{p.Name}:{p.ParameterType.Name}")
            .ToArray();

        Assert.Empty(forbidden);
    }
}
