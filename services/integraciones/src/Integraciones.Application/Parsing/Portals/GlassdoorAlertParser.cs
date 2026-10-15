using System.Text.RegularExpressions;
using Integraciones.Application.Mail;

namespace Integraciones.Application.Parsing.Portals;

/// <summary>
/// Correos "Ofertas de empleo para ti" de Glassdoor. Solo traen HTML. Cada oferta es un enlace que envuelve
/// toda la tarjeta y cuya URL lleva <c>jobListingId</c>, el id de la oferta. Dentro, por orden: empresa,
/// valoración ("2.6 ★"), título, ubicación y a veces salario o "Solicitud rápida".
/// </summary>
public sealed partial class GlassdoorAlertParser : JobAlertParser
{
    public override string Portal => "Glassdoor";

    public override IReadOnlyCollection<string> Senders { get; } = ["noreply@glassdoor.com"];

    protected override IEnumerable<ParsedJobOffer> ParseOffers(EmailMessage message)
    {
        var document = HtmlTools.Parse(message.HtmlBody);
        if (document is null)
        {
            yield break; // por ejemplo, los correos de noticias de empresas no traen ofertas
        }

        foreach (var link in document.QuerySelectorAll("a[href*='jobListingId=']"))
        {
            var id = JobListingId().Match(link.GetAttribute("href") ?? string.Empty);
            if (!id.Success)
            {
                continue;
            }

            var texts = HtmlTools.LeafTexts(link)
                .Where(t => !Rating().IsMatch(t) && !t.Equals("Solicitud rápida", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (texts.Count < 2)
            {
                continue;
            }

            var company = texts[0];
            var title = texts[1];
            var rest = texts.Skip(2).ToList();
            var salary = rest.FirstOrDefault(t => t.Contains('€') || t.Contains('$'));
            var location = rest.FirstOrDefault(t => t != salary);

            var jobListingId = id.Groups["id"].Value;
            yield return new ParsedJobOffer(
                ExternalId: $"gd-{jobListingId}",
                Portal: Portal,
                Title: title,
                Company: company,
                Location: location,
                WorkMode: TextTools.DetectWorkMode(location, title),
                Salary: salary,
                Url: $"https://www.glassdoor.es/job-listing/j?jl={jobListingId}",
                Description: null);
        }
    }

    [GeneratedRegex(@"jobListingId=(?<id>\d+)")]
    private static partial Regex JobListingId();

    [GeneratedRegex(@"^\d+([.,]\d+)?\s*★$")]
    private static partial Regex Rating();
}
