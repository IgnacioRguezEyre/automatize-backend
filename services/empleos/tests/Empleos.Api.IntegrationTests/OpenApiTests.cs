using System.Net;
using System.Text.Json;

namespace Empleos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OpenApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task The_OpenAPI_document_describes_the_endpoints()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/job-offers", out _));
        Assert.True(paths.TryGetProperty("/api/job-offers/import", out _));
        Assert.True(paths.TryGetProperty("/api/sources", out _));
        Assert.Equal("Automatize · Empleos API", document.RootElement.GetProperty("info").GetProperty("title").GetString());
    }

    [Fact]
    public async Task Swagger_UI_is_served_in_development()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
