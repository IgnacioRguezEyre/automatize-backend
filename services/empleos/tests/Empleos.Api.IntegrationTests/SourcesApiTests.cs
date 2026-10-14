using System.Net;
using System.Net.Http.Json;
using Empleos.Application.Sources;

namespace Empleos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class SourcesApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    /// <summary>Nombre único por test: todos comparten la misma base de datos y el nombre no se puede repetir.</summary>
    private static string UniqueName() => $"Portal {Guid.NewGuid():N}"[..20];

    private async Task<SourceDto> CreateSource(string name, params string[] senders)
    {
        var response = await _client.PostAsJsonAsync("/api/sources", new CreateSourceRequest(name, senders, null), Json.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SourceDto>(Json.Options))!;
    }

    [Fact]
    public async Task Create_stores_the_senders_and_they_come_back_on_read()
    {
        var source = await CreateSource(UniqueName(), "alertas@portal.com", "Ofertas@Portal.com");

        var read = await _client.GetFromJsonAsync<SourceDto>($"/api/sources/{source.Id}", Json.Options);

        Assert.Equal(new[] { "alertas@portal.com", "ofertas@portal.com" }, read!.Senders);
        Assert.True(read.IsActive);
    }

    [Fact]
    public async Task Create_with_a_repeated_name_returns_409()
    {
        var name = UniqueName();
        await CreateSource(name);

        var response = await _client.PostAsJsonAsync("/api/sources", new CreateSourceRequest(name, null, null), Json.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_an_invalid_sender_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/sources",
            new CreateSourceRequest(UniqueName(), ["no-es-un-correo"], null), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_and_remove_a_sender_is_persisted()
    {
        var source = await CreateSource(UniqueName(), "a@portal.com");

        await _client.PostAsJsonAsync($"/api/sources/{source.Id}/senders", new AddSenderRequest("b@portal.com"), Json.Options);
        await _client.DeleteAsync($"/api/sources/{source.Id}/senders/a@portal.com");

        var read = await _client.GetFromJsonAsync<SourceDto>($"/api/sources/{source.Id}", Json.Options);
        Assert.Equal(new[] { "b@portal.com" }, read!.Senders);
    }

    [Fact]
    public async Task Deactivate_and_delete()
    {
        var source = await CreateSource(UniqueName());

        var deactivated = await (await _client.PostAsync($"/api/sources/{source.Id}/deactivate", null))
            .Content.ReadFromJsonAsync<SourceDto>(Json.Options);
        var deleted = await _client.DeleteAsync($"/api/sources/{source.Id}");
        var afterDelete = await _client.GetAsync($"/api/sources/{source.Id}");

        Assert.False(deactivated!.IsActive);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }
}
