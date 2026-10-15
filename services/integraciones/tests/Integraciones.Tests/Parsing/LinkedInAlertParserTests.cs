using Integraciones.Application.Parsing.Portals;
using Integraciones.Tests.Fakes;

namespace Integraciones.Tests.Parsing;

public class LinkedInAlertParserTests
{
    private const string Sender = "jobalerts-noreply@linkedin.com";
    private readonly LinkedInAlertParser _parser = new();

    [Fact]
    public void Extracts_every_job_of_the_alert()
    {
        var offers = _parser.Parse(Mail.Text(Sender, "linkedin-alert.txt"));

        Assert.Equal(6, offers.Count);
        var first = offers[0];
        Assert.Equal("li-4476480239", first.ExternalId);
        Assert.Equal("LinkedIn", first.Portal);
        Assert.Equal("API Operations Engineer", first.Title);
        Assert.Equal("Ria Money Transfer", first.Company);
        Assert.Equal("Alcobendas", first.Location);
        Assert.Equal("https://www.linkedin.com/jobs/view/4476480239/", first.Url);
    }

    [Fact]
    public void Separates_the_salary_that_LinkedIn_appends_to_the_title()
    {
        var offer = _parser.Parse(Mail.Text(Sender, "linkedin-alert.txt")).Single(o => o.ExternalId == "li-4478404970");

        Assert.Equal(".NET Developer", offer.Title);
        Assert.Equal("€42K - €50K", offer.Salary);
        Assert.Equal("Barcelona y alrededores", offer.Location);
    }

    [Fact]
    public void Detects_remote_work_from_the_title()
    {
        var offer = _parser.Parse(Mail.Text(Sender, "linkedin-alert.txt")).Single(o => o.ExternalId == "li-4476593548");

        Assert.Equal("Team Leader NET Teletrabajo", offer.Title);
        Assert.Equal("Remoto", offer.WorkMode);
    }

    [Fact]
    public void Works_with_windows_line_endings()
    {
        var message = Mail.Text(Sender, "linkedin-alert.txt");
        var crlf = message with { TextBody = message.TextBody!.ReplaceLineEndings("\r\n") };

        Assert.Equal(_parser.Parse(message), _parser.Parse(crlf));
    }

    [Fact]
    public void Rejects_mail_from_other_senders()
    {
        var message = Mail.Text("donotreply@match.indeed.com", "linkedin-alert.txt");

        Assert.False(_parser.CanParse(message));
        Assert.Throws<ArgumentException>(() => _parser.Parse(message));
    }
}
