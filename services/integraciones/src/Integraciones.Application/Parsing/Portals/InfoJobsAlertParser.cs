using Integraciones.Application.Mail;

namespace Integraciones.Application.Parsing.Portals;

/// <summary>
/// Alertas de InfoJobs ("Estas son las ofertas de empleo que hemos encontrado..."). Solo HTML.
/// El título es un enlace cuyo atributo <c>name</c> lleva el código de la oferta (<c>push-of-i6c4ab...</c>);
/// debajo, en celdas <c>td.text</c>, la empresa y una línea "Ciudad | Modalidad | Contrato | Salario".
/// </summary>
public sealed class InfoJobsAlertParser : JobAlertParser
{
    private const string NamePrefix = "push-of-";

    public override string Portal => "InfoJobs";

    public override IReadOnlyCollection<string> Senders { get; } = ["ofertas@push.infojobs.net"];

    protected override IEnumerable<ParsedJobOffer> ParseOffers(EmailMessage message)
    {
        var document = HtmlTools.Parse(message.HtmlBody);
        if (document is null)
        {
            yield break;
        }

        foreach (var link in document.QuerySelectorAll($"a[name^='{NamePrefix}']"))
        {
            var code = link.GetAttribute("name")![NamePrefix.Length..];
            var title = TextTools.Clean(link.TextContent);
            if (code.Length == 0 || title is null)
            {
                continue;
            }

            var cells = link.Closest("table")?.QuerySelectorAll("td.text")
                .Select(td => TextTools.Clean(td.TextContent))
                .ToList() ?? [];
            var company = cells.ElementAtOrDefault(0);
            var details = (cells.ElementAtOrDefault(1) ?? string.Empty)
                .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var location = details.FirstOrDefault();
            var salary = details.FirstOrDefault(d => d.Contains('€'));
            var workMode = details.Skip(1).Select(d => TextTools.DetectWorkMode(d)).FirstOrDefault(m => m is not null);
            var contract = details.Skip(1).Where(d => d != salary && TextTools.DetectWorkMode(d) is null).ToList();

            yield return new ParsedJobOffer(
                ExternalId: $"ij-{code}",
                Portal: Portal,
                Title: title,
                Company: company,
                Location: location,
                WorkMode: workMode,
                Salary: salary,
                Url: link.GetAttribute("href"), // enlace del correo: redirige a la oferta
                Description: contract.Count == 0 ? null : $"Contrato: {string.Join(" · ", contract)}");
        }
    }
}
