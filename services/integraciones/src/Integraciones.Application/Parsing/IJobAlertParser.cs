using Integraciones.Application.Mail;

namespace Integraciones.Application.Parsing;

/// <summary>
/// Un analizador por portal (patrón Strategy): cada uno sabe reconocer sus correos y sacar las ofertas.
/// Añadir un portal nuevo es añadir una clase, sin tocar las demás ni el servicio que las usa.
/// </summary>
public interface IJobAlertParser
{
    /// <summary>Nombre del portal tal como se guarda en Empleos (LinkedIn, Glassdoor...).</summary>
    string Portal { get; }

    /// <summary>Remitentes de las alertas de este portal. Con ellos se arma la búsqueda en el buzón.</summary>
    IReadOnlyCollection<string> Senders { get; }

    bool CanParse(EmailMessage message);

    /// <summary>
    /// Ofertas del correo. Una lista vacía es válida: el mismo remitente también manda correos que no son
    /// ofertas (noticias de empresas, encuestas...).
    /// </summary>
    IReadOnlyList<ParsedJobOffer> Parse(EmailMessage message);
}
