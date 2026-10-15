namespace Integraciones.Application.Mail;

/// <summary>
/// Un correo ya descargado, sin nada específico de Gmail: así los analizadores se pueden probar con
/// ficheros de ejemplo y el día de mañana se podría leer de otro buzón (Outlook, IMAP...).
/// </summary>
/// <param name="Id">Identificador del correo en el buzón de origen.</param>
/// <param name="From">Dirección del remitente, sin el nombre (ejemplo: <c>jobalerts-noreply@linkedin.com</c>).</param>
/// <param name="TextBody">Parte de texto plano, si el correo la trae.</param>
/// <param name="HtmlBody">Parte HTML, si el correo la trae.</param>
public sealed record EmailMessage(
    string Id,
    string From,
    string Subject,
    DateTimeOffset ReceivedAt,
    string? TextBody,
    string? HtmlBody);
