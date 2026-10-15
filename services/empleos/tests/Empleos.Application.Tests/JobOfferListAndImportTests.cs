using Empleos.Application.JobOffers;
using Empleos.Application.Tests.Fakes;
using Empleos.Domain.JobOffers;

namespace Empleos.Application.Tests;

public class JobOfferListAndImportTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);
    private readonly InMemoryJobOfferRepository _repository = new();
    private readonly JobOfferService _service;

    public JobOfferListAndImportTests() => _service = new JobOfferService(_repository, new FixedClock(Today));

    private static ImportJobOfferItem Item(
        string externalId,
        JobOfferStatus? status = null,
        DateOnly? statusDate = null,
        IReadOnlyList<ImportApplicationEvent>? events = null,
        string? url = null) =>
        new(externalId, $"Oferta {externalId}", "Empresa", "LinkedIn", new DateOnly(2026, 10, 1), status, statusDate,
            "Vigo", "Remoto", null, url, null, null, IsClosed: false, events);

    // ---------- Listado ----------

    [Fact]
    public async Task List_returns_the_requested_page_and_the_total()
    {
        for (var i = 0; i < 25; i++)
            await _service.CreateAsync(new CreateJobOfferRequest($"Oferta {i}", "Empresa", "LinkedIn", null), default);

        var page2 = await _service.ListAsync(new JobOfferQuery(Page: 2, PageSize: 10), default);
        var page3 = await _service.ListAsync(new JobOfferQuery(Page: 3, PageSize: 10), default);

        Assert.Equal(10, page2.Items.Count);
        Assert.Equal(5, page3.Items.Count);
        Assert.Equal(25, page2.TotalCount);
        Assert.Equal(3, page2.TotalPages);
        Assert.True(page2.HasNextPage);
        Assert.False(page3.HasNextPage);
    }

    [Fact]
    public async Task List_searches_title_and_company_ignoring_case()
    {
        await _service.CreateAsync(new CreateJobOfferRequest("Backend .NET", "ALTECH", "LinkedIn", null), default);
        await _service.CreateAsync(new CreateJobOfferRequest("Java Developer", "Plexus", "LinkedIn", null), default);

        var byTitle = await _service.ListAsync(new JobOfferQuery(Search: ".net"), default);
        var byCompany = await _service.ListAsync(new JobOfferQuery(Search: "plexus"), default);

        Assert.Equal("Backend .NET", Assert.Single(byTitle.Items).Title);
        Assert.Equal("Java Developer", Assert.Single(byCompany.Items).Title);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, JobOfferQuery.MaxPageSize + 1)]
    public async Task List_with_an_invalid_page_fails(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ListAsync(new JobOfferQuery(Page: page, PageSize: pageSize), default));
    }

    // ---------- Importación ----------

    [Fact]
    public async Task Import_creates_the_offers_and_saves_once()
    {
        var result = await _service.ImportAsync([Item("li-1"), Item("li-2"), Item("gd-3")], default);

        Assert.Equal(3, result.Received);
        Assert.Equal(3, result.Created);
        Assert.Empty(result.Errors);
        Assert.Equal(3, _repository.Offers.Count);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Import_twice_does_not_duplicate()
    {
        await _service.ImportAsync([Item("li-1"), Item("li-2")], default);

        var second = await _service.ImportAsync([Item("li-1"), Item("LI-2"), Item("li-3")], default);

        Assert.Equal(1, second.Created);
        Assert.Equal(2, second.Skipped);
        Assert.Equal(3, _repository.Offers.Count);
    }

    [Fact]
    public async Task Import_ignores_a_repeated_id_inside_the_same_batch()
    {
        var result = await _service.ImportAsync([Item("li-1"), Item("li-1")], default);

        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public async Task Import_rebuilds_status_events_notes_and_closed()
    {
        var item = Item("in-fritz", JobOfferStatus.Applied, new DateOnly(2026, 10, 3),
            [new ImportApplicationEvent(new DateOnly(2026, 10, 6), ApplicationEventType.CvRead, "CV leído")])
            with { Notes = "Contacto: Laura", IsClosed = true };

        await _service.ImportAsync([item], default);

        var offer = Assert.Single(_repository.Offers);
        Assert.Equal(JobOfferStatus.Applied, offer.Status);
        Assert.Equal(new DateOnly(2026, 10, 3), offer.StatusDate);
        Assert.Equal(ApplicationEventType.CvRead, Assert.Single(offer.Events).Type);
        Assert.Equal("Contacto: Laura", offer.Notes);
        Assert.True(offer.IsClosed);
        Assert.Equal("in-fritz", offer.ExternalId);
        Assert.Equal("Vigo", offer.Location);
    }

    [Fact]
    public async Task Import_applies_the_domain_rules_a_rejection_discards()
    {
        var item = Item("li-9", JobOfferStatus.Applied, new DateOnly(2026, 10, 2),
            [new ImportApplicationEvent(new DateOnly(2026, 10, 8), ApplicationEventType.Rejection, "Otros perfiles")]);

        await _service.ImportAsync([item], default);

        Assert.Equal(JobOfferStatus.Discarded, Assert.Single(_repository.Offers).Status);
    }

    [Fact]
    public async Task Import_reports_invalid_offers_and_keeps_the_valid_ones()
    {
        var result = await _service.ImportAsync([Item("li-1"), Item("li-2", url: "no-es-una-url"), Item("")], default);

        Assert.Equal(1, result.Created);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ExternalId == "li-2");
        Assert.Single(_repository.Offers);
    }

    [Fact]
    public async Task Import_with_nothing_new_does_not_save()
    {
        await _service.ImportAsync([Item("li-1")], default);

        await _service.ImportAsync([Item("li-1")], default);

        Assert.Equal(1, _repository.SaveCount);
    }
}
