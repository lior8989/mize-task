using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SupplierFeed.Tests;

/// <summary>
/// Verifies the API host starts and exposes its Swagger documents.
/// </summary>
public class SwaggerSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerSmokeTests(WebApplicationFactory<Program> factory)
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
