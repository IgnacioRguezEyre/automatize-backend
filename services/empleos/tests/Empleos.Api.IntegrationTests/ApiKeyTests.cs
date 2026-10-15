using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Empleos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ApiKeyTests(ApiFactory factory)
{
    private const string Key = "clave-de-prueba";

    // Una copia de la API con la clave configurada, como estará en Azure.
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(builder => builder.UseSetting("Security:ApiKey", Key))
        .CreateClient();

    [Fact]
    public async Task Without_the_key_the_api_returns_401()
    {
        var response = await _client.GetAsync("/api/sources");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task With_a_wrong_key_the_api_returns_401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sources");
        request.Headers.Add("X-Api-Key", "otra-clave");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task With_the_right_key_the_api_answers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sources");
        request.Headers.Add("X-Api-Key", Key);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_health_check_does_not_need_the_key()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
