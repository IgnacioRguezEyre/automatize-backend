using System.Net;
using System.Net.Http.Json;
using Empleos.Application.JobOffers;
using Empleos.Domain.JobOffers;

namespace Empleos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class JobOffersApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<JobOfferDto> CreateOffer(string title = "Backend Developer (.NET)")
    {
        var response = await _client.PostAsJsonAsync("/api/job-offers",
            new CreateJobOfferRequest(title, "ALTECH", "LinkedIn", null), Json.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JobOfferDto>(Json.Options))!;
    }

    [Fact]
    public async Task Create_returns_201_with_location_and_the_offer_can_be_read_back()
    {
        var response = await _client.PostAsJsonAsync("/api/job-offers",
            new CreateJobOfferRequest("Backend Developer", "ALTECH", "LinkedIn", new DateOnly(2026, 10, 1)), Json.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<JobOfferDto>(Json.Options))!;
        Assert.NotNull(response.Headers.Location);

        var read = await _client.GetFromJsonAsync<JobOfferDto>(response.Headers.Location, Json.Options);
        Assert.Equal(created.Id, read!.Id);
        Assert.Equal(JobOfferStatus.New, read.Status);
    }

    [Fact]
    public async Task Create_without_title_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/job-offers",
            new CreateJobOfferRequest("", null, "LinkedIn", null), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_an_unknown_offer_returns_404()
    {
        var response = await _client.GetAsync($"/api/job-offers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Apply_to_a_discarded_offer_returns_409()
    {
        var offer = await CreateOffer();
        await _client.PostAsync($"/api/job-offers/{offer.Id}/discard", null);

        var response = await _client.PostAsync($"/api/job-offers/{offer.Id}/apply", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task List_filtered_by_status_only_returns_that_status()
    {
        var viewed = await CreateOffer("Oferta vista");
        await _client.PostAsync($"/api/job-offers/{viewed.Id}/view", null);

        var offers = await _client.GetFromJsonAsync<List<JobOfferDto>>("/api/job-offers?status=viewed", Json.Options);

        Assert.Contains(offers!, o => o.Id == viewed.Id);
        Assert.All(offers!, o => Assert.Equal(JobOfferStatus.Viewed, o.Status));
    }

    [Fact]
    public async Task List_with_an_unknown_status_returns_400()
    {
        var response = await _client.GetAsync("/api/job-offers?status=Archivada");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Events_and_notes_are_stored_in_the_database()
    {
        var offer = await CreateOffer();

        await _client.PostAsJsonAsync($"/api/job-offers/{offer.Id}/events",
            new AddApplicationEventRequest(ApplicationEventType.Interview, null, "Entrevista técnica"), Json.Options);
        await _client.PutAsJsonAsync($"/api/job-offers/{offer.Id}/notes",
            new UpdateNotesRequest("Contacto: Laura"), Json.Options);

        // Se vuelve a leer desde la base de datos para comprobar que el mapeo de EF guarda y carga las novedades.
        var read = await _client.GetFromJsonAsync<JobOfferDto>($"/api/job-offers/{offer.Id}", Json.Options);
        Assert.Equal(JobOfferStatus.Applied, read!.Status);
        Assert.Equal("Contacto: Laura", read.Notes);
        var e = Assert.Single(read.Events);
        Assert.Equal(ApplicationEventType.Interview, e.Type);
        Assert.Equal("Entrevista técnica", e.Text);
    }

    [Fact]
    public async Task A_rejection_discards_the_offer()
    {
        var offer = await CreateOffer();

        var response = await _client.PostAsJsonAsync($"/api/job-offers/{offer.Id}/events",
            new AddApplicationEventRequest(ApplicationEventType.Rejection, null, "Siguen con otros perfiles"), Json.Options);

        var updated = await response.Content.ReadFromJsonAsync<JobOfferDto>(Json.Options);
        Assert.Equal(JobOfferStatus.Discarded, updated!.Status);
    }

    [Fact]
    public async Task Close_and_reopen()
    {
        var offer = await CreateOffer();

        var closed = await (await _client.PostAsync($"/api/job-offers/{offer.Id}/close", null))
            .Content.ReadFromJsonAsync<JobOfferDto>(Json.Options);
        var reopened = await (await _client.PostAsync($"/api/job-offers/{offer.Id}/reopen", null))
            .Content.ReadFromJsonAsync<JobOfferDto>(Json.Options);

        Assert.True(closed!.IsClosed);
        Assert.False(reopened!.IsClosed);
    }
}
