using System.Text.RegularExpressions;
using Integraciones.Application.Mail;

namespace Integraciones.Application.Parsing.Portals;

/// <summary>
/// Correos "Parece que tienes la trayectoria ideal para este puesto" de Indeed: una oferta por correo.
/// En el texto plano vienen título, empresa, ubicación y luego líneas "Clave: valor" (Salario, Modalidad...).
/// El enlace del correo es de seguimiento y no deja ver el id de la oferta, así que el identificador se
/// arma con la empresa y el título, igual que hacía la página de Automatize.
/// </summary>
public sealed partial class IndeedAlertParser : JobAlertParser
{
    public override string Portal => "Indeed";

    // Ojo: indeedapply@indeed.com también escribe, pero son confirmaciones de candidatura, no ofertas.
    public override IReadOnlyCollection<string> Senders { get; } = ["donotreply@match.indeed.com"];

    protected override IEnumerable<ParsedJobOffer> ParseOffers(EmailMessage message)
    {
        var text = message.TextBody;
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var viewLink = ViewLink().Match(text);
        if (!viewLink.Success)
        {
            yield break;
        }

        // La tarjeta es el último párrafo antes de "Ver empleo" que empieza por el título
        // (el de "Prestaciones:" va después, se descarta abajo).
        var paragraphs = BlankLine().Split(text[..viewLink.Index])
            .Select(p => p.Split('\n').Select(TextTools.Clean).OfType<string>().ToArray())
            .Where(lines => lines.Length >= 2 && !lines[0].EndsWith(':') && !lines[0].StartsWith("Hola", StringComparison.OrdinalIgnoreCase)
                            && !lines[0].StartsWith("Parece que", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (paragraphs.Count == 0)
        {
            yield break;
        }

        var card = paragraphs[0];
        var title = card[0];
        var company = card[1];
        string? location = null, salary = null, workMode = null, jobType = null;
        foreach (var line in card.Skip(2))
        {
            var field = Field().Match(line);
            if (!field.Success)
            {
                location ??= line;
                continue;
            }

            var value = field.Groups["value"].Value.Trim();
            switch (field.Groups["key"].Value)
            {
                case "Salario": salary = value; break;
                case "Modalidad de trabajo": workMode = value; break;
                case "Tipo de empleo": jobType = value; break;
            }
        }

        yield return new ParsedJobOffer(
            ExternalId: TextTools.ExternalIdFromText("in", company, title),
            Portal: Portal,
            Title: title,
            Company: company,
            Location: location,
            WorkMode: TextTools.DetectWorkMode(workMode, location, title) ?? workMode,
            Salary: salary,
            Url: viewLink.Groups["url"].Value,
            Description: jobType is null ? null : $"Tipo de empleo: {jobType}");
    }

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLine();

    [GeneratedRegex(@"^Ver empleo:\s*(?<url>https?://\S+)", RegexOptions.Multiline)]
    private static partial Regex ViewLink();

    [GeneratedRegex(@"^(?<key>Salario|Tipo de empleo|Modalidad de trabajo):\s*(?<value>.+)$")]
    private static partial Regex Field();
}
