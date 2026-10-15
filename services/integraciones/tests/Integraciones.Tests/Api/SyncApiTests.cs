using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Integraciones.Application.Empleos;
using Integraciones.Application.Mail;
using Integraciones.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Integraciones.Tests.Api;

/// <summary>
/// Arranca la API de verdad en memoria (WebApplicationFactory) cambiando solo los dos adaptadores externos:
/// el buzón y el cliente de Empleos. Así se prueban rutas, inyección de dependencias y JSON de punta a punta.
/// </summary>
public class SyncApiTests
{
    private readonly FakeEmpleosClient _empleos = new();

    private HttpClient CreateClient(IMailboxReader mailbox, string? apiKey = null)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Empleos:BaseUrl", "http://empleos.test");
            builder.UseSetting("Sync:IntervalMinutes", "0"); // sin sincronización automática durante los tests
            if (apiKey is not null)
            {
                builder.UseSetting("Security:ApiKey", apiKey);
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMailboxReader>();
                services.AddSingleton(mailbox);
                services.RemoveAll<IEmpleosClient>();
                services.AddSingleton<IEmpleosClient>(_empleos);
            });
        });
        return factory.CreateClient();
    }

    [Fact]
    public async Task Sync_reads_the_mailbox_and_imports_into_Empleos()
    {
        var client = CreateClient(new FakeMailbox(Mail.Html("alertas@push.tecnoempleo.com", "tecnoempleo-alert.html")));

        var response = await client.PostAsync("/api/sync", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, report.GetProperty("offersFound").GetInt32());
        Assert.Equal(3, report.GetProperty("import").GetProperty("created").GetInt32());
        Assert.Equal(3, Assert.Single(_empleos.Calls).Count);
    }

    [Fact]
    public async Task Dry_run_returns_offers_and_sends_nothing()
    {
        var client = CreateClient(new FakeMailbox(Mail.Text("donotreply@match.indeed.com", "indeed-match.txt")));

        var response = await client.PostAsync("/api/sync?dryRun=true", null);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();

        var offer = Assert.Single(report.GetProperty("offers").EnumerateArray().ToList());
        Assert.Equal("Programmatore / Sviluppatore", offer.GetProperty("title").GetString());
        Assert.Empty(_empleos.Calls);
    }

    [Fact]
    public async Task Query_endpoint_shows_the_gmail_search()
    {
        var client = CreateClient(new FakeMailbox());

        var query = await client.GetFromJsonAsync<string>("/api/sync/query");

        Assert.Contains("newer_than:", query);
    }

    [Fact]
    public async Task Api_key_is_required_when_configured_but_health_stays_open()
    {
        var client = CreateClient(new FakeMailbox(), apiKey: "secreta");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/sync", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
}
