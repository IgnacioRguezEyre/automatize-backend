using System.Net;
using System.Text.Json;
using Integraciones.Application.Empleos;
using Integraciones.Infrastructure;
using Integraciones.Infrastructure.Empleos;
using Integraciones.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integraciones.Tests.Infrastructure;

public class EmpleosClientTests
{
    private const string OkResult = """{"received":1,"created":1,"skipped":0,"errors":[]}""";

    private static readonly ImportJobOfferItem Item = new(
        "li-1", ".NET Developer", "Joppy", "LinkedIn", new DateOnly(2026, 10, 15),
        "Barcelona", "Remoto", "€42K - €50K", "https://www.linkedin.com/jobs/view/1/", null);

    /// <summary>Monta el cliente igual que en producción (AddInfrastructure), pero con la red simulada.</summary>
    private static IEmpleosClient CreateClient(StubHandler handler, string? apiKey = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Empleos:BaseUrl"] = "http://empleos.test/base",
            ["Empleos:ApiKey"] = apiKey
        }).Build();

        var services = new ServiceCollection().AddLogging().AddInfrastructure(configuration);
        services.AddHttpClient<IEmpleosClient, EmpleosClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider().GetRequiredService<IEmpleosClient>();
    }

    [Fact]
    public async Task Posts_the_offers_to_the_import_endpoint_as_camel_case_json()
    {
        var handler = new StubHandler(() => StubHandler.Json(OkResult));

        var result = await CreateClient(handler).ImportAsync([Item], CancellationToken.None);

        Assert.Equal(1, result.Created);
        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://empleos.test/base/api/job-offers/import", request.RequestUri!.ToString());

        using var json = JsonDocument.Parse(body!);
        var sent = Assert.Single(json.RootElement.EnumerateArray().ToList());
        Assert.Equal("li-1", sent.GetProperty("externalId").GetString());
        Assert.Equal("2026-10-15", sent.GetProperty("arrivalDate").GetString());
    }

    [Fact]
    public async Task Sends_the_api_key_header_when_configured()
    {
        var handler = new StubHandler(() => StubHandler.Json(OkResult));

        await CreateClient(handler, apiKey: "secreta").ImportAsync([Item], CancellationToken.None);

        var (request, _) = Assert.Single(handler.Requests);
        Assert.Equal("secreta", Assert.Single(request.Headers.GetValues(EmpleosClient.ApiKeyHeader)));
    }

    [Fact]
    public async Task Retries_transient_errors()
    {
        var handler = new StubHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => StubHandler.Json(OkResult));

        var result = await CreateClient(handler).ImportAsync([Item], CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(1, result.Created);
    }

    [Fact]
    public async Task Does_not_retry_bad_requests_and_reports_the_body()
    {
        var handler = new StubHandler(() => StubHandler.Json("""{"title":"Datos no válidos"}""", HttpStatusCode.BadRequest));

        var error = await Assert.ThrowsAsync<EmpleosApiException>(() => CreateClient(handler).ImportAsync([Item], CancellationToken.None));

        Assert.Single(handler.Requests);
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Contains("Datos no válidos", error.Message);
    }
}
