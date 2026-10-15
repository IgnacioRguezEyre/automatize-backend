using Integraciones.Application.Parsing.Portals;
using Integraciones.Tests.Fakes;

namespace Integraciones.Tests.Parsing;

public class IndeedAlertParserTests
{
    private const string Sender = "donotreply@match.indeed.com";
    private readonly IndeedAlertParser _parser = new();

    [Fact]
    public void Extracts_the_single_job_of_the_email()
    {
        var offer = Assert.Single(_parser.Parse(Mail.Text(Sender, "indeed-match.txt")));

        Assert.Equal("Programmatore / Sviluppatore", offer.Title);
        Assert.Equal("Luca e Paolo barra srl", offer.Company);
        Assert.Equal("84131 Salerno", offer.Location);
        Assert.Equal("Tipo de empleo: Tempo pieno", offer.Description);
        Assert.Null(offer.Salary);
        Assert.Equal("https://cts.indeed.com/v3/redacted", offer.Url);
    }

    [Fact]
    public void Builds_the_id_from_company_and_title_like_the_old_page_did()
    {
        var offer = Assert.Single(_parser.Parse(Mail.Text(Sender, "indeed-match.txt")));

        Assert.Equal("in-luca-e-paolo-barra-srl-programmatore-sviluppatore", offer.ExternalId);
    }

    [Fact]
    public void Reads_salary_and_work_mode_when_present()
    {
        var offer = Assert.Single(_parser.Parse(Mail.Text(Sender, "indeed-match-salary.txt")));

        Assert.Equal("Senior Software Developer", offer.Title);
        Assert.Equal("Glenair Italia S.P.A.", offer.Company);
        Assert.Equal("Bologna, Emilia-Romagna", offer.Location);
        Assert.Equal("De 29.830 € a 44.000 € al año", offer.Salary);
        Assert.Equal("Presencial", offer.WorkMode); // "In presenza"
    }

    [Fact]
    public void Ignores_application_confirmations()
    {
        Assert.False(_parser.CanParse(Mail.Text("indeedapply@indeed.com", "indeed-match.txt")));
    }
}
