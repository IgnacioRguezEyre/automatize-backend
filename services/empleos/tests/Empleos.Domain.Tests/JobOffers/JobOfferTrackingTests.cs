using Empleos.Domain.JobOffers;

namespace Empleos.Domain.Tests.JobOffers;

/// <summary>Seguimiento de la candidatura: novedades, notas y oferta cerrada.</summary>
public class JobOfferTrackingTests
{
    private static readonly DateOnly Arrival = new(2026, 10, 1);
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static JobOffer NewOffer() =>
        JobOffer.Create("Backend Developer (.NET)", "ALTECH", "LinkedIn", Arrival);

    [Fact]
    public void AddEvent_on_a_new_offer_marks_it_applied_on_the_event_date()
    {
        var offer = NewOffer();

        var added = offer.AddEvent(Today, ApplicationEventType.CvRead, "Tecnoempleo: CV leído");

        Assert.True(added);
        Assert.Equal(JobOfferStatus.Applied, offer.Status);
        Assert.Equal(Today, offer.StatusDate);
        var e = Assert.Single(offer.Events);
        Assert.Equal(ApplicationEventType.CvRead, e.Type);
        Assert.Equal("Tecnoempleo: CV leído", e.Text);
    }

    [Fact]
    public void AddEvent_keeps_the_original_applied_date()
    {
        var offer = NewOffer();
        offer.MarkApplied(Arrival.AddDays(1));

        offer.AddEvent(Today, ApplicationEventType.Interview, "Entrevista el lunes");

        Assert.Equal(Arrival.AddDays(1), offer.StatusDate);
    }

    [Fact]
    public void AddEvent_rejection_discards_the_offer()
    {
        var offer = NewOffer();
        offer.MarkApplied(Arrival);

        offer.AddEvent(Today, ApplicationEventType.Rejection, "Siguen con otros perfiles");

        Assert.Equal(JobOfferStatus.Discarded, offer.Status);
        Assert.Equal(Today, offer.StatusDate);
    }

    [Fact]
    public void AddEvent_with_the_same_type_and_date_is_ignored()
    {
        var offer = NewOffer();
        offer.AddEvent(Today, ApplicationEventType.Response, "Primera");

        var added = offer.AddEvent(Today, ApplicationEventType.Response, "Repetida");

        Assert.False(added);
        Assert.Single(offer.Events);
    }

    [Fact]
    public void AddEvent_of_another_type_the_same_day_is_added()
    {
        var offer = NewOffer();
        offer.AddEvent(Today, ApplicationEventType.CvRead, null);

        offer.AddEvent(Today, ApplicationEventType.Response, null);

        Assert.Equal(2, offer.Events.Count);
    }

    [Fact]
    public void AddEvent_before_the_arrival_date_fails()
    {
        var offer = NewOffer();

        Assert.Throws<ArgumentException>(() => offer.AddEvent(Arrival.AddDays(-1), ApplicationEventType.Response, null));
        Assert.Empty(offer.Events);
    }

    [Fact]
    public void AddEvent_with_a_text_that_is_too_long_fails()
    {
        var offer = NewOffer();
        var text = new string('x', ApplicationEvent.MaxTextLength + 1);

        Assert.Throws<ArgumentException>(() => offer.AddEvent(Today, ApplicationEventType.Response, text));
    }

    [Fact]
    public void AddEvent_trims_the_text_and_accepts_null()
    {
        var offer = NewOffer();

        offer.AddEvent(Today, ApplicationEventType.Response, "  Hola  ");
        offer.AddEvent(Today, ApplicationEventType.Interview, null);

        Assert.Equal(new[] { "Hola", "" }, offer.Events.Select(e => e.Text));
    }

    [Fact]
    public void UpdateNotes_trims_and_null_clears_them()
    {
        var offer = NewOffer();

        offer.UpdateNotes("  Salario hablado: 40.000 €  ");
        Assert.Equal("Salario hablado: 40.000 €", offer.Notes);

        offer.UpdateNotes(null);
        Assert.Equal(string.Empty, offer.Notes);
    }

    [Fact]
    public void UpdateNotes_too_long_fails_and_keeps_the_old_notes()
    {
        var offer = NewOffer();
        offer.UpdateNotes("Notas");

        Assert.Throws<ArgumentException>(() => offer.UpdateNotes(new string('x', JobOffer.MaxNotesLength + 1)));
        Assert.Equal("Notas", offer.Notes);
    }

    [Fact]
    public void Close_and_Reopen_toggle_IsClosed_without_touching_the_status()
    {
        var offer = NewOffer();

        offer.Close();
        Assert.True(offer.IsClosed);
        Assert.Equal(JobOfferStatus.New, offer.Status);

        offer.Reopen();
        Assert.False(offer.IsClosed);
    }
}
