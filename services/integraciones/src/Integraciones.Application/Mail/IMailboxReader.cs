namespace Integraciones.Application.Mail;

/// <summary>Puerto de salida hacia el buzón. La implementación real está en Infrastructure (Gmail API).</summary>
public interface IMailboxReader
{
    /// <summary>
    /// Devuelve los correos que cumplen la búsqueda, como mucho <paramref name="maxMessages"/>.
    /// La sintaxis de <paramref name="query"/> es la del buscador de Gmail (<c>from:</c>, <c>newer_than:</c>...).
    /// </summary>
    Task<IReadOnlyList<EmailMessage>> SearchAsync(string query, int maxMessages, CancellationToken cancellationToken);
}
