using Integraciones.Application.Mail;

namespace Integraciones.Tests.Fakes;

/// <summary>Construye correos de prueba a partir de los ficheros de Fixtures.</summary>
internal static class Mail
{
    public static readonly DateTimeOffset ReceivedAt = new(2026, 10, 15, 8, 13, 0, TimeSpan.Zero);

    public static string Read(string fixture) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture));

    public static EmailMessage Text(string from, string fixture, DateTimeOffset? receivedAt = null) =>
        new(Guid.NewGuid().ToString("N"), from, "Alerta", receivedAt ?? ReceivedAt, Read(fixture), null);

    public static EmailMessage Html(string from, string fixture, DateTimeOffset? receivedAt = null) =>
        new(Guid.NewGuid().ToString("N"), from, "Alerta", receivedAt ?? ReceivedAt, null, Read(fixture));
}
