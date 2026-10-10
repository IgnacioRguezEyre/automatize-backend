using Empleos.Domain.JobOffers;

namespace Empleos.Domain.Tests.JobOffers;

public class JobOfferTests
{
    private static readonly DateOnly Arrival = new(2026, 10, 1);
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static JobOffer NewOffer() =>
        JobOffer.Create("Backend Developer (.NET)", "ALTECH", "LinkedIn", Arrival);

    [Fact]
    public void Create_leaves_the_offer_as_new_without_status_date()
    {
        var offer = NewOffer();

        Assert.Equal(JobOfferStatus.New, offer.Status);
        Assert.Null(offer.StatusDate);
        Assert.Equal(Arrival, offer.ArrivalDate);
        Assert.NotEqual(Guid.Empty, offer.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_without_title_fails(string title)
    {
        Assert.Throws<ArgumentException>(() => JobOffer.Create(title, "ALTECH", "LinkedIn", Arrival));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_without_portal_fails(string portal)
    {
        Assert.Throws<ArgumentException>(() => JobOffer.Create("Backend Developer", "ALTECH", portal, Arrival));
    }

    [Fact]
    public void Create_allows_a_missing_company()
    {
        var offer = JobOffer.Create("Backend Developer", null, "Indeed", Arrival);

        Assert.Equal(string.Empty, offer.Company);
    }

    [Fact]
    public void MarkViewed_from_new_moves_to_viewed_with_the_date()
    {
        var offer = NewOffer();

        offer.MarkViewed(Today);

        Assert.Equal(JobOfferStatus.Viewed, offer.Status);
        Assert.Equal(Today, offer.StatusDate);
    }

    [Fact]
    public void MarkViewed_does_not_change_an_applied_offer()
    {
        var offer = NewOffer();
        offer.MarkApplied(Today);

        offer.MarkViewed(Today.AddDays(1));

        Assert.Equal(JobOfferStatus.Applied, offer.Status);
        Assert.Equal(Today, offer.StatusDate);
    }

    [Fact]
    public void MarkApplied_works_from_new_and_from_viewed()
    {
        var fromNew = NewOffer();
        var fromViewed = NewOffer();
        fromViewed.MarkViewed(Today);

        fromNew.MarkApplied(Today);
        fromViewed.MarkApplied(Today);

        Assert.Equal(JobOfferStatus.Applied, fromNew.Status);
        Assert.Equal(JobOfferStatus.Applied, fromViewed.Status);
    }

    [Fact]
    public void MarkApplied_twice_keeps_the_first_date()
    {
        var offer = NewOffer();
        offer.MarkApplied(Today);

        offer.MarkApplied(Today.AddDays(3));

        Assert.Equal(Today, offer.StatusDate);
    }

    [Fact]
    public void MarkApplied_on_a_discarded_offer_fails()
    {
        var offer = NewOffer();
        offer.Discard(Today);

        Assert.Throws<InvalidOperationException>(() => offer.MarkApplied(Today));
        Assert.Equal(JobOfferStatus.Discarded, offer.Status);
    }

    [Fact]
    public void UnmarkApplied_goes_back_to_viewed_never_to_new()
    {
        var offer = NewOffer();
        offer.MarkApplied(Today);

        offer.UnmarkApplied(Today.AddDays(1));

        Assert.Equal(JobOfferStatus.Viewed, offer.Status);
        Assert.Equal(Today.AddDays(1), offer.StatusDate);
    }

    [Fact]
    public void UnmarkApplied_does_nothing_if_it_was_not_applied()
    {
        var offer = NewOffer();

        offer.UnmarkApplied(Today);

        Assert.Equal(JobOfferStatus.New, offer.Status);
        Assert.Null(offer.StatusDate);
    }

    [Theory]
    [InlineData(JobOfferStatus.New)]
    [InlineData(JobOfferStatus.Viewed)]
    [InlineData(JobOfferStatus.Applied)]
    public void Discard_works_from_any_status(JobOfferStatus initial)
    {
        var offer = NewOffer();
        if (initial == JobOfferStatus.Viewed) offer.MarkViewed(Today);
        if (initial == JobOfferStatus.Applied) offer.MarkApplied(Today);

        offer.Discard(Today.AddDays(1));

        Assert.Equal(JobOfferStatus.Discarded, offer.Status);
        Assert.Equal(Today.AddDays(1), offer.StatusDate);
    }

    [Fact]
    public void Restore_moves_a_discarded_offer_back_to_viewed()
    {
        var offer = NewOffer();
        offer.Discard(Today);

        offer.Restore(Today.AddDays(1));

        Assert.Equal(JobOfferStatus.Viewed, offer.Status);
    }

    [Fact]
    public void Restore_does_nothing_if_it_was_not_discarded()
    {
        var offer = NewOffer();

        offer.Restore(Today);

        Assert.Equal(JobOfferStatus.New, offer.Status);
    }

    [Theory]
    [InlineData(13, false)]
    [InlineData(14, true)]
    [InlineData(30, true)]
    public void IsStale_from_14_days_on_while_still_pending(int days, bool expected)
    {
        var offer = NewOffer();
        offer.MarkViewed(Arrival);

        Assert.Equal(expected, offer.IsStale(Arrival.AddDays(days)));
    }

    [Fact]
    public void IsStale_is_false_once_applied_or_discarded()
    {
        var applied = NewOffer();
        applied.MarkApplied(Arrival);
        var discarded = NewOffer();
        discarded.Discard(Arrival);

        Assert.False(applied.IsStale(Arrival.AddDays(60)));
        Assert.False(discarded.IsStale(Arrival.AddDays(60)));
    }
}
