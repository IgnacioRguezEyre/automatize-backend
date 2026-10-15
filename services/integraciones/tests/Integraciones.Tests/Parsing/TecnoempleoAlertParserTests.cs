using Integraciones.Application.Parsing.Portals;
using Integraciones.Tests.Fakes;

namespace Integraciones.Tests.Parsing;

public class TecnoempleoAlertParserTests
{
    private const string Sender = "alertas@push.tecnoempleo.com";
    private readonly TecnoempleoAlertParser _parser = new();

    [Fact]
    public void Extracts_offers_using_the_url_inside_the_tracking_link()
    {
        var offers = _parser.Parse(Mail.Html(Sender, "tecnoempleo-alert.html"));

        Assert.Equal(3, offers.Count);
        var first = offers[0];
        Assert.Equal("te-b68813916269830c314b", first.ExternalId);
        Assert.Equal("Desarrollador SENIOR .Net", first.Title);
        Assert.Equal("SlashMobility", first.Company);
        Assert.Equal("Barcelona", first.Location);
        Assert.Equal("https://www.tecnoempleo.com/desarrollador-senior-net/net-azure/rf-b68813916269830c314b", first.Url);
        Assert.Equal("Tecnologías: .NET, Azure · Jorn. completa · Indefinido", first.Description);
    }

    [Fact]
    public void Cleans_trailing_commas_in_technologies()
    {
        var offer = _parser.Parse(Mail.Html(Sender, "tecnoempleo-alert.html"))[2];

        Assert.Equal("te-0ea9171ab29073b8f74c", offer.ExternalId);
        Assert.Equal("Tecnologías: data, SQL, BI · Jorn. completa · Indefinido", offer.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://www.tecnoempleo.com/tracking.php?cid=no-es-un-jwt")]
    [InlineData("https://www.tecnoempleo.com/tracking.php?cid=a.%%%.b")]
    public void Invalid_tracking_links_are_ignored(string? href)
    {
        Assert.Null(TecnoempleoAlertParser.DestinationOf(href));
    }
}
