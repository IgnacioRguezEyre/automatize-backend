using Integraciones.Application.Mail;

namespace Integraciones.Application.Parsing;

/// <summary>Base común: reconoce el correo por el remitente y deja a cada portal solo la extracción.</summary>
public abstract class JobAlertParser : IJobAlertParser
{
    public abstract string Portal { get; }

    public abstract IReadOnlyCollection<string> Senders { get; }

    public bool CanParse(EmailMessage message) =>
        Senders.Contains(message.From.Trim(), StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ParsedJobOffer> Parse(EmailMessage message)
    {
        if (!CanParse(message))
        {
            throw new ArgumentException($"El correo de {message.From} no es una alerta de {Portal}.", nameof(message));
        }

        // Una misma oferta puede aparecer dos veces en el mismo correo (por ejemplo, en dos secciones).
        return ParseOffers(message)
            .Where(o => !string.IsNullOrWhiteSpace(o.Title))
            .DistinctBy(o => o.ExternalId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    protected abstract IEnumerable<ParsedJobOffer> ParseOffers(EmailMessage message);
}
