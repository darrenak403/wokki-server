using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Wokki.Api.IntegrationTests;

public class HealthEndpointTests : IClassFixture<WokkiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WokkiWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
