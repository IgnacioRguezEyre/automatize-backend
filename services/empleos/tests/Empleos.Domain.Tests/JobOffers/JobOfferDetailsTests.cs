using Empleos.Domain.JobOffers;

namespace Empleos.Domain.Tests.JobOffers;

public class JobOfferDetailsTests
{
    private static readonly DateOnly Arrival = new(2026, 10, 1);

    [Fact]
    public void Create_with_details_trims_them_and_turns_blanks_into_null()
    {
        var details = new JobOfferDetails(Location: "  Vigo ", WorkMode: "", Url: "https://www.linkedin.com/jobs/view/1/");

        var offer = JobOffer.Create("Backend Developer", "ALTECH", "LinkedIn", Arrival, details, externalId: " li-1 ");

        Assert.Equal("Vigo", offer.Location);
        Assert.Null(offer.WorkMode);
        Assert.Null(offer.Salary);
        Assert.Equal("https://www.linkedin.com/jobs/view/1/", offer.Url);
        Assert.Equal("li-1", offer.ExternalId);
    }

    [Fact]
    public void Create_without_details_leaves_them_empty()
    {
        var offer = JobOffer.Create("Backend Developer", "ALTECH", "LinkedIn", Arrival);

        Assert.Null(offer.Location);
        Assert.Null(offer.ExternalId);
    }

    [Theory]
    [InlineData("no-es-una-url")]
    [InlineData("ftp://servidor/oferta")]
    [InlineData("javascript:alert(1)")]
    public void A_url_that_is_not_http_fails(string url)
    {
        Assert.Throws<ArgumentException>(() =>
            JobOffer.Create("Backend Developer", null, "LinkedIn", Arrival, new JobOfferDetails(Url: url)));
    }

    [Fact]
    public void UpdateDetails_with_invalid_data_changes_nothing()
    {
        var offer = JobOffer.Create("Backend Developer", null, "LinkedIn", Arrival, new JobOfferDetails(Location: "Vigo"));

        Assert.Throws<ArgumentException>(() =>
            offer.UpdateDetails(new JobOfferDetails(Location: "Madrid", Url: "no-es-una-url")));
        Assert.Equal("Vigo", offer.Location);
    }

    [Fact]
    public void A_description_that_is_too_long_fails()
    {
        var text = new string('x', JobOfferDetails.MaxDescriptionLength + 1);

        Assert.Throws<ArgumentException>(() =>
            JobOffer.Create("Backend Developer", null, "LinkedIn", Arrival, new JobOfferDetails(Description: text)));
    }

    [Fact]
    public void An_external_id_that_is_too_long_fails()
    {
        var id = new string('x', JobOffer.MaxExternalIdLength + 1);

        Assert.Throws<ArgumentException>(() => JobOffer.Create("Backend Developer", null, "LinkedIn", Arrival, externalId: id));
    }
}
