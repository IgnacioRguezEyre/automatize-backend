using System.Buffers.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Integraciones.Application.Mail;

namespace Integraciones.Application.Parsing.Portals;

/// <summary>
/// Alertas de Tecnoempleo ("Hola, hoy hemos encontrado las siguientes ofertas..."). Se lee el HTML:
/// cada oferta es una tabla con el título enlazado, "Empresa - Ciudad" y "Tecnologías | Jornada | Contrato".
/// El enlace pasa por <c>tracking.php?cid=...</c>, donde <c>cid</c> es un JWT cuyo contenido (<c>dest</c>)
/// es la URL real de la oferta. De ella sale el id (<c>rf-xxxxxxxx</c>).
/// </summary>
public sealed partial class TecnoempleoAlertParser : JobAlertParser
{
    public override string Portal => "Tecnoempleo";

    public override IReadOnlyCollection<string> Senders { get; } = ["alertas@push.tecnoempleo.com"];

    protected override IEnumerable<ParsedJobOffer> ParseOffers(EmailMessage message)
    {
        var document = HtmlTools.Parse(message.HtmlBody);
        if (document is null)
        {
            yield break;
        }

        foreach (var link in document.QuerySelectorAll("a[href*='tracking.php?cid=']"))
        {
            var url = DestinationOf(link.GetAttribute("href"));
            var reference = url is null ? null : Reference().Match(url);
            var title = TextTools.Clean(link.TextContent);
            if (reference is not { Success: true } || title is null)
            {
                continue; // enlaces de seguimiento que no son ofertas (cursos, publicidad...)
            }

            var rows = link.Closest("table")?.QuerySelectorAll("tr").ToList() ?? [];
            var companyCell = rows.ElementAtOrDefault(1)?.QuerySelector("td");
            var company = companyCell is null ? null : HtmlTools.OwnText(companyCell)?.TrimEnd('-', ' ');
            var location = companyCell is null ? null : TextTools.Clean(companyCell.QuerySelector("span")?.TextContent);
            var details = TextTools.Clean(rows.ElementAtOrDefault(2)?.TextContent);

            yield return new ParsedJobOffer(
                ExternalId: $"te-{reference.Groups["id"].Value}",
                Portal: Portal,
                Title: title,
                Company: string.IsNullOrEmpty(company) ? null : company,
                Location: location,
                WorkMode: TextTools.DetectWorkMode(location, title, details),
                Salary: null,
                Url: url,
                Description: Describe(details));
        }
    }

    /// <summary>Saca la URL de destino del JWT del enlace. No se comprueba la firma: solo se lee.</summary>
    internal static string? DestinationOf(string? href)
    {
        var cid = href is null ? null : Cid().Match(href);
        if (cid is not { Success: true })
        {
            return null;
        }

        var parts = cid.Groups["jwt"].Value.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = Base64Url.DecodeFromChars(parts[1]);
            using var json = JsonDocument.Parse(payload);
            var dest = json.RootElement.TryGetProperty("dest", out var value) ? value.GetString() : null;
            return dest?.Split('?')[0];
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return null;
        }
    }

    /// <summary>".NET,  Azure | Jorn. completa | Indefinido" → "Tecnologías: .NET, Azure · Jorn. completa · Indefinido".</summary>
    private static string? Describe(string? details)
    {
        if (details is null)
        {
            return null;
        }

        var parts = details.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count == 0)
        {
            return null;
        }

        var technologies = string.Join(", ", parts[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        return string.Join(" · ", new[] { $"Tecnologías: {technologies}" }.Concat(parts.Skip(1)));
    }

    [GeneratedRegex(@"[?&]cid=(?<jwt>[\w\-.]+)")]
    private static partial Regex Cid();

    [GeneratedRegex(@"/rf-(?<id>[0-9a-f]+)$")]
    private static partial Regex Reference();
}
