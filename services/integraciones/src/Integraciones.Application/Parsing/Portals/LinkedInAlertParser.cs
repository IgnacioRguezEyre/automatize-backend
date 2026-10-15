using System.Text.RegularExpressions;
using Integraciones.Application.Mail;

namespace Integraciones.Application.Parsing.Portals;

/// <summary>
/// Alertas de LinkedIn ("Tu alerta de empleo para..."). Se lee la parte de texto plano, que es muy regular:
/// cada oferta es un bloque separado por una línea de guiones, con título, empresa y ubicación en las tres
/// primeras líneas y el enlace al final ("Ver anuncio de empleo: https://www.linkedin.com/comm/jobs/view/123/...").
/// </summary>
public sealed partial class LinkedInAlertParser : JobAlertParser
{
    public override string Portal => "LinkedIn";

    public override IReadOnlyCollection<string> Senders { get; } = ["jobalerts-noreply@linkedin.com"];

    protected override IEnumerable<ParsedJobOffer> ParseOffers(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.TextBody))
        {
            yield break;
        }

        foreach (var block in Separator().Split(message.TextBody))
        {
            var link = JobLink().Match(block);
            if (!link.Success)
            {
                continue; // pie del correo, "Ver todos los empleos"...
            }

            var card = FindCard(block[..link.Index]);
            if (card is null)
            {
                continue;
            }

            var jobId = link.Groups["id"].Value;
            var (title, salary) = SplitSalary(card[0]);
            yield return new ParsedJobOffer(
                ExternalId: $"li-{jobId}",
                Portal: Portal,
                Title: title,
                Company: card[1],
                Location: card[2],
                WorkMode: TextTools.DetectWorkMode(card[2], title),
                Salary: salary,
                Url: $"https://www.linkedin.com/jobs/view/{jobId}/", // sin los parámetros de seguimiento del correo
                Description: null);
        }
    }

    /// <summary>
    /// Dentro del bloque, la tarjeta es el primer párrafo (líneas seguidas sin hueco) con al menos tres líneas.
    /// Así se salta la cabecera del primer bloque ("Tu alerta de empleo para..." y "Más de 30 nuevos empleos...").
    /// </summary>
    private static string[]? FindCard(string text)
    {
        var paragraphs = BlankLine().Split(text)
            .Select(p => p.Split('\n').Select(TextTools.Clean).OfType<string>().ToArray())
            .Where(lines => lines.Length > 0);

        return paragraphs.FirstOrDefault(lines =>
            lines.Length >= 3 && !lines[0].StartsWith("Tu alerta de empleo", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>"Programador/a .NET | €40K - €60K" → título "Programador/a .NET" y salario "€40K - €60K".</summary>
    private static (string Title, string? Salary) SplitSalary(string line)
    {
        var bar = line.LastIndexOf(" | ", StringComparison.Ordinal);
        if (bar > 0)
        {
            var right = line[(bar + 3)..];
            if (right.IndexOfAny(['€', '$', '£']) >= 0)
            {
                return (line[..bar].Trim(), right.Trim());
            }
        }

        return (line, null);
    }

    [GeneratedRegex(@"^-{10,}\s*$", RegexOptions.Multiline)]
    private static partial Regex Separator();

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLine();

    [GeneratedRegex(@"Ver anuncio de empleo:\s*https?://[^\s]*/jobs/view/(?<id>\d+)")]
    private static partial Regex JobLink();
}
