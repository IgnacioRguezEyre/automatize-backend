using System.Buffers.Text;
using System.Net.Mail;
using System.Text;
using Google.Apis.Gmail.v1.Data;
using Integraciones.Application.Mail;

namespace Integraciones.Infrastructure.Gmail;

/// <summary>
/// Traduce un mensaje de la API de Gmail (formato <c>full</c>) a <see cref="EmailMessage"/>.
/// Gmail entrega el correo como un árbol de partes MIME (multipart/alternative con text/plain y text/html,
/// a veces anidadas) y el contenido de cada parte en Base64 "URL-safe".
/// </summary>
internal static class GmailMessageMapper
{
    public static EmailMessage ToEmailMessage(Message message)
    {
        var headers = message.Payload?.Headers ?? [];
        string? Header(string name) => headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

        var receivedAt = message.InternalDate is { } millis
            ? DateTimeOffset.FromUnixTimeMilliseconds(millis)
            : DateTimeOffset.UtcNow;

        return new EmailMessage(
            Id: message.Id,
            From: AddressOf(Header("From")),
            Subject: Header("Subject") ?? string.Empty,
            ReceivedAt: receivedAt,
            TextBody: FindBody(message.Payload, "text/plain"),
            HtmlBody: FindBody(message.Payload, "text/html"));
    }

    /// <summary>"Alertas de empleo de LinkedIn &lt;jobalerts-noreply@linkedin.com&gt;" → "jobalerts-noreply@linkedin.com".</summary>
    internal static string AddressOf(string? from)
    {
        if (string.IsNullOrWhiteSpace(from))
        {
            return string.Empty;
        }

        return MailAddress.TryCreate(from, out var address) ? address.Address : from.Trim();
    }

    /// <summary>Busca en profundidad la primera parte del tipo pedido que no sea un adjunto.</summary>
    private static string? FindBody(MessagePart? part, string mimeType)
    {
        if (part is null)
        {
            return null;
        }

        if (string.Equals(part.MimeType, mimeType, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(part.Filename)
            && part.Body?.Data is { Length: > 0 } data)
        {
            return Decode(data);
        }

        foreach (var child in part.Parts ?? [])
        {
            var body = FindBody(child, mimeType);
            if (body is not null)
            {
                return body;
            }
        }

        return null;
    }

    // Gmail ya deshace el quoted-printable o base64 del correo; lo que queda son los bytes del texto,
    // que en las alertas de los portales siempre vienen en UTF-8.
    private static string Decode(string base64Url) =>
        Encoding.UTF8.GetString(Base64Url.DecodeFromChars(base64Url.TrimEnd('=')));
}
