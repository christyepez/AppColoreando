using AppColoreando.Application.Contracts;

namespace AppColoreando.IntegrationTests;

public sealed class ContractSmokeTests
{
    [Fact]
    public void Catalog_query_defaults_are_safe_for_pagination()
    {
        var query = new CatalogQuery(null, null, null, null, null, null);

        Assert.Equal(1, query.Page);
        Assert.Equal(24, query.PageSize);
    }
}
