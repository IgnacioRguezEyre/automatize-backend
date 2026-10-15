using Integraciones.Application.Mail;
using Integraciones.Application.Parsing.Portals;
using Integraciones.Tests.Fakes;

namespace Integraciones.Tests.Parsing;

public class GlassdoorAlertParserTests
{
    private const string Sender = "noreply@glassdoor.com";
    private readonly GlassdoorAlertParser _parser = new();

    [Fact]
    public void Extracts_every_job_card()
    {
        var offers = _parser.Parse(Mail.Html(Sender, "glassdoor-jobs-for-you.html"));

        Assert.Equal(5, offers.Count);
        var first = offers[0];
        Assert.Equal("gd-1010270354816", first.ExternalId);
        Assert.Equal("Full stack .net", first.Title);
        Assert.Equal("Tecdata Engineering", first.Company);
        Assert.Equal("Barcelona", first.Location);
        Assert.Equal("https://www.glassdoor.es/job-listing/j?jl=1010270354816", first.Url);
    }

    [Fact]
    public void Skips_rating_and_quick_apply_labels()
    {
        var offers = _parser.Parse(Mail.Html(Sender, "glassdoor-jobs-for-you.html"));

        Assert.DoesNotContain(offers, o => o.Title.Contains('★') || o.Location == "Solicitud rápida");
        var dapin = Assert.Single(offers, o => o.Company == "DAPIN");
        Assert.Equal("Full Stack .Net Developer", dapin.Title);
        Assert.Equal("Spain", dapin.Location);
    }

    [Fact]
    public void Detects_remote_jobs()
    {
        var offer = Assert.Single(_parser.Parse(Mail.Html(Sender, "glassdoor-jobs-for-you.html")),
            o => o.Title == "Desarrollador/a Java Full Stack (Remoto)");

        Assert.Equal("Remoto", offer.WorkMode);
    }

    [Fact]
    public void Returns_nothing_for_emails_without_jobs()
    {
        // Glassdoor también manda noticias de empresas desde el mismo remitente.
        var news = new EmailMessage("1", Sender, "Lo último de Microsoft", Mail.ReceivedAt, null,
            "<html><body><p>Opiniones de esta semana</p><a href=\"https://www.glassdoor.es/Opiniones\">Ver más</a></body></html>");

        Assert.Empty(_parser.Parse(news));
    }
}
