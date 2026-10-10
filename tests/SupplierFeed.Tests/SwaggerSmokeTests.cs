using System.Net;
using SupplierFeed.Tests.TestSupport;

namespace SupplierFeed.Tests;

/// <summary>
/// Verifies the API host starts and exposes its Swagger documents.
/// </summary>
public class SwaggerSmokeTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SwaggerSmokeTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerJson_IsServed()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
