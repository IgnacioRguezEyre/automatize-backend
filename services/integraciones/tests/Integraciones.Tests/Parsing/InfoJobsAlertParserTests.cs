using Integraciones.Application.Parsing.Portals;
using Integraciones.Tests.Fakes;

namespace Integraciones.Tests.Parsing;

public class InfoJobsAlertParserTests
{
    private const string Sender = "ofertas@push.infojobs.net";
    private readonly InfoJobsAlertParser _parser = new();

    [Fact]
    public void Extracts_every_offer_with_its_code()
    {
        var offers = _parser.Parse(Mail.Html(Sender, "infojobs-alert.html"));

        Assert.Equal(10, offers.Count);
        var first = offers[0];
        Assert.Equal("ij-i6c4abeb0f94f25b2316ea485b63ddf", first.ExternalId);
        Assert.Equal("Técnico/a informático/a", first.Title);
        Assert.Equal("SMARTERS ACCESORIOS S.L.", first.Company);
        Assert.Equal("Sevilla", first.Location);
        Assert.Equal("Presencial", first.WorkMode);
        Assert.Equal("1.100€ - 1.200€ Bruto/mes", first.Salary);
        Assert.Equal("Contrato: De duración determinada", first.Description);
    }

    [Fact]
    public void Offers_with_the_same_title_are_still_different_offers()
    {
        var offers = _parser.Parse(Mail.Html(Sender, "infojobs-alert.html"));

        Assert.Equal(3, offers.Count(o => o.Title == "Técnico/a informático/a"));
    }

    [Fact]
    public void Reads_hybrid_work_and_missing_work_mode()
    {
        var offers = _parser.Parse(Mail.Html(Sender, "infojobs-alert.html"));

        var hybrid = Assert.Single(offers, o => o.Company == "MRInformatica");
        Assert.Equal("Híbrido", hybrid.WorkMode);
        Assert.Equal("28.000€ - 40.000€ Bruto/año", hybrid.Salary);

        var noMode = Assert.Single(offers, o => o.Company == "Adecco");
        Assert.Null(noMode.WorkMode);
        Assert.Equal("Don Benito", noMode.Location);
    }
}
