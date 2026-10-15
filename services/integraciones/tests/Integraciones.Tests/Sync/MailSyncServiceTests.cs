using Integraciones.Application.Mail;
using Integraciones.Application.Parsing;
using Integraciones.Application.Parsing.Portals;
using Integraciones.Application.Sync;
using Integraciones.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Integraciones.Tests.Sync;

public class MailSyncServiceTests
{
    private static readonly IJobAlertParser[] Parsers =
    [
        new LinkedInAlertParser(), new IndeedAlertParser(), new GlassdoorAlertParser(),
        new InfoJobsAlertParser(), new TecnoempleoAlertParser()
    ];

    private readonly FakeEmpleosClient _empleos = new();

    private MailSyncService CreateService(IMailboxReader mailbox, params IJobAlertParser[] parsers) =>
        new(mailbox, parsers.Length > 0 ? parsers : Parsers, _empleos,
            Options.Create(new SyncOptions { LookbackDays = 3, TimeZone = "Europe/Madrid" }),
            new FixedClock(Mail.ReceivedAt), NullLogger<MailSyncService>.Instance);

    [Fact]
    public void Query_includes_every_portal_sender_and_the_lookback()
    {
        var query = CreateService(new FakeMailbox()).BuildQuery();

        Assert.StartsWith("from:(jobalerts-noreply@linkedin.com OR donotreply@match.indeed.com", query);
        Assert.Contains("alertas@push.tecnoempleo.com", query);
        Assert.EndsWith("newer_than:3d", query);
    }

    [Fact]
    public async Task Sends_the_offers_of_every_portal_to_Empleos()
    {
        var mailbox = new FakeMailbox(
            Mail.Text("jobalerts-noreply@linkedin.com", "linkedin-alert.txt"),
            Mail.Text("donotreply@match.indeed.com", "indeed-match.txt"),
            Mail.Html("noreply@glassdoor.com", "glassdoor-jobs-for-you.html"),
            Mail.Html("ofertas@push.infojobs.net", "infojobs-alert.html"),
            Mail.Html("alertas@push.tecnoempleo.com", "tecnoempleo-alert.html"));

        var report = await CreateService(mailbox).RunAsync(dryRun: false, CancellationToken.None);

        Assert.Equal(5, report.MessagesRead);
        Assert.Equal(5, report.MessagesWithOffers);
        Assert.Equal(6 + 1 + 5 + 10 + 3, report.OffersFound);
        Assert.Equal(10, report.OffersByPortal["InfoJobs"]);
        Assert.Equal(report.OffersFound, report.Import!.Created);
        Assert.Null(report.Offers);

        var sent = Assert.Single(_empleos.Calls);
        Assert.Equal(report.OffersFound, sent.Count);
    }

    [Fact]
    public async Task Arrival_date_uses_the_configured_time_zone()
    {
        // 23:30 UTC del 14 de octubre es ya 15 de octubre en Madrid (UTC+2 en verano).
        var lateNight = new DateTimeOffset(2026, 10, 14, 23, 30, 0, TimeSpan.Zero);
        var mailbox = new FakeMailbox(Mail.Text("donotreply@match.indeed.com", "indeed-match.txt", lateNight));

        await CreateService(mailbox).RunAsync(dryRun: false, CancellationToken.None);

        var item = Assert.Single(Assert.Single(_empleos.Calls));
        Assert.Equal(new DateOnly(2026, 10, 15), item.ArrivalDate);
    }

    [Fact]
    public async Task Repeated_offers_are_sent_once_with_the_date_of_the_first_email()
    {
        var mailbox = new FakeMailbox(
            Mail.Text("donotreply@match.indeed.com", "indeed-match.txt", Mail.ReceivedAt),
            Mail.Text("donotreply@match.indeed.com", "indeed-match.txt", Mail.ReceivedAt.AddDays(-2)));

        var report = await CreateService(mailbox).RunAsync(dryRun: false, CancellationToken.None);

        Assert.Equal(1, report.OffersFound);
        var item = Assert.Single(Assert.Single(_empleos.Calls));
        Assert.Equal(DateOnly.FromDateTime(Mail.ReceivedAt.AddDays(-2).UtcDateTime), item.ArrivalDate);
    }

    [Fact]
    public async Task Dry_run_returns_the_offers_without_calling_Empleos()
    {
        var mailbox = new FakeMailbox(Mail.Html("alertas@push.tecnoempleo.com", "tecnoempleo-alert.html"));

        var report = await CreateService(mailbox).RunAsync(dryRun: true, CancellationToken.None);

        Assert.True(report.DryRun);
        Assert.Equal(3, report.Offers!.Count);
        Assert.Null(report.Import);
        Assert.Empty(_empleos.Calls);
    }

    [Fact]
    public async Task A_broken_parser_does_not_stop_the_others()
    {
        var mailbox = new FakeMailbox(
            Mail.Text("roto@example.com", "indeed-match.txt"),
            Mail.Text("donotreply@match.indeed.com", "indeed-match.txt"));

        var report = await CreateService(mailbox, new ThrowingParser(), new IndeedAlertParser())
            .RunAsync(dryRun: false, CancellationToken.None);

        Assert.Equal(1, report.MessagesFailed);
        Assert.Equal(1, report.OffersFound);
    }

    [Fact]
    public async Task Unknown_senders_and_empty_emails_are_skipped_and_nothing_is_sent()
    {
        var mailbox = new FakeMailbox(
            Mail.Text("newsletter@example.com", "indeed-match.txt"),
            new EmailMessage("x", "noreply@glassdoor.com", "Noticias", Mail.ReceivedAt, null, "<p>Sin ofertas</p>"));

        var report = await CreateService(mailbox).RunAsync(dryRun: false, CancellationToken.None);

        Assert.Equal(2, report.MessagesRead);
        Assert.Equal(0, report.MessagesWithOffers);
        Assert.Null(report.Import);
        Assert.Empty(_empleos.Calls);
    }

    [Fact]
    public async Task Running_twice_creates_nothing_the_second_time()
    {
        var mailbox = new FakeMailbox(Mail.Text("jobalerts-noreply@linkedin.com", "linkedin-alert.txt"));
        var service = CreateService(mailbox);

        await service.RunAsync(dryRun: false, CancellationToken.None);
        var second = await service.RunAsync(dryRun: false, CancellationToken.None);

        Assert.Equal(0, second.Import!.Created);
        Assert.Equal(6, second.Import.Skipped);
    }

    private sealed class ThrowingParser : IJobAlertParser
    {
        public string Portal => "Roto";
        public IReadOnlyCollection<string> Senders { get; } = ["roto@example.com"];
        public bool CanParse(EmailMessage message) => message.From == "roto@example.com";
        public IReadOnlyList<ParsedJobOffer> Parse(EmailMessage message) => throw new FormatException("Formato nuevo");
    }
}
