using Empleos.Application.JobOffers;
using Empleos.Application.Tests.Fakes;
using Empleos.Domain.Common;
using Empleos.Domain.JobOffers;

namespace Empleos.Application.Tests;

public class JobOfferServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);
    private readonly InMemoryJobOfferRepository _repository = new();
    private readonly JobOfferService _service;

    public JobOfferServiceTests() => _service = new JobOfferService(_repository, new FixedClock(Today));

    private async Task<JobOfferDto> CreateOffer(DateOnly? arrival = null) =>
        await _service.CreateAsync(new CreateJobOfferRequest("Backend Developer", "ALTECH", "LinkedIn", arrival), default);

    [Fact]
    public async Task Create_saves_the_offer_and_uses_today_when_there_is_no_arrival_date()
    {
        var offer = await CreateOffer();

        Assert.Equal(Today, offer.ArrivalDate);
        Assert.Equal(JobOfferStatus.New, offer.Status);
        Assert.Single(_repository.Offers);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task List_marks_stale_offers_using_the_clock()
    {
        await CreateOffer(Today.AddDays(-20));
        await CreateOffer(Today.AddDays(-2));

        var result = await _service.ListAsync(new JobOfferQuery(), default);

        Assert.Equal(1, result.Items.Count(o => o.IsStale));
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        var applied = await CreateOffer();
        await CreateOffer();
        await _service.ChangeAsync(applied.Id, (o, today) => o.MarkApplied(today), default);

        var result = await _service.ListAsync(new JobOfferQuery(Status: JobOfferStatus.Applied), default);

        Assert.Equal(applied.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Change_on_a_missing_offer_returns_null_and_saves_nothing()
    {
        var result = await _service.ChangeAsync(Guid.NewGuid(), (o, today) => o.MarkViewed(today), default);

        Assert.Null(result);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task Change_that_breaks_a_rule_throws_and_saves_nothing()
    {
        var offer = await CreateOffer();
        await _service.ChangeAsync(offer.Id, (o, today) => o.Discard(today), default);
        var savesBefore = _repository.SaveCount;

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.ChangeAsync(offer.Id, (o, today) => o.MarkApplied(today), default));
        Assert.Equal(savesBefore, _repository.SaveCount);
    }

    [Fact]
    public async Task AddEvent_without_date_uses_today_and_marks_the_offer_applied()
    {
        var offer = await CreateOffer(Today.AddDays(-3));

        var result = await _service.AddEventAsync(offer.Id, new AddApplicationEventRequest(ApplicationEventType.Response, null, "Piden disponibilidad"), default);

        Assert.NotNull(result);
        Assert.Equal(JobOfferStatus.Applied, result.Status);
        var e = Assert.Single(result.Events);
        Assert.Equal(Today, e.Date);
        Assert.Equal("Piden disponibilidad", e.Text);
    }

    [Fact]
    public async Task AddEvent_without_type_fails()
    {
        var offer = await CreateOffer();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddEventAsync(offer.Id, new AddApplicationEventRequest(null, null, "Sin tipo"), default));
    }

    [Fact]
    public async Task UpdateNotes_saves_the_notes()
    {
        var offer = await CreateOffer();

        var result = await _service.UpdateNotesAsync(offer.Id, new UpdateNotesRequest("Contacto: Laura"), default);

        Assert.Equal("Contacto: Laura", result!.Notes);
        Assert.Equal(2, _repository.SaveCount);
    }
}
