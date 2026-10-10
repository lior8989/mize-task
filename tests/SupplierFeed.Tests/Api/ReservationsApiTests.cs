using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SupplierFeed.Tests.TestSupport;

namespace SupplierFeed.Tests.Api;

public class ReservationsApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ReservationsApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object Body(string supplier, string id = "r1", decimal price = 450m, string checkOut = "2026-08-03T00:00:00Z") => new
    {
        supplierId = supplier,
        reservationId = id,
        roomId = "room-1",
        checkIn = "2026-08-01T00:00:00Z",
        checkOut,
        price,
        updatedAtUtc = "2026-07-01T10:15:00Z"
    };

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    [Fact]
    public async Task Ingest_NewThenDuplicateThenChange_ReturnsDistinguishableResponses()
    {
        var created = await _client.PostAsJsonAsync("/api/reservations/ingest", Body("api-a"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("Created", (await Json(created)).GetProperty("outcome").GetString());

        var duplicate = await _client.PostAsJsonAsync("/api/reservations/ingest", Body("api-a"));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        var dup = await Json(duplicate);
        Assert.Equal("Unchanged", dup.GetProperty("outcome").GetString());
        Assert.False(dup.GetProperty("changed").GetBoolean());

        var updated = await _client.PostAsJsonAsync("/api/reservations/ingest", Body("api-a", price: 500m));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.True((await Json(updated)).GetProperty("changed").GetBoolean());
    }

    [Fact]
    public async Task Ingest_InvalidBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/reservations/ingest",
            Body("api-b", checkOut: "2026-07-01T00:00:00Z"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_MissingSupplier_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/reservations/ingest", Body(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_NegativePrice_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/reservations/ingest", Body("api-b", price: -1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_101stRequestInAWindow_Returns429WithRetryAfter_AndStatsReflectIt()
    {
        for (var i = 0; i < 100; i++)
        {
            var ok = await _client.PostAsJsonAsync("/api/reservations/ingest", Body("api-c", $"r{i}"));
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        }

        var throttled = await _client.PostAsJsonAsync("/api/reservations/ingest", Body("api-c", "r100"));

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.True(throttled.Headers.RetryAfter?.Delta?.TotalSeconds is > 0 and <= 60);

        var stats = await Json(await _client.GetAsync("/api/reservations/stats/api-c"));
        Assert.Equal(100, stats.GetProperty("ingested").GetInt64());
        Assert.Equal(1, stats.GetProperty("throttled").GetInt64());
    }

    [Fact]
    public async Task Stats_UnknownSupplier_ReturnsZeros()
    {
        var response = await _client.GetAsync("/api/reservations/stats/never-seen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await Json(response);
        Assert.Equal(0, stats.GetProperty("ingested").GetInt64());
        Assert.Equal(0, stats.GetProperty("throttled").GetInt64());
    }
}
