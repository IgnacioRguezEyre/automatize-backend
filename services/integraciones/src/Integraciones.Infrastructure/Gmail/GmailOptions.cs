namespace Integraciones.Infrastructure.Gmail;

/// <summary>
/// Credenciales OAuth de Gmail (sección <c>Gmail</c>). Nunca van en appsettings: en local se guardan con
/// dotnet user-secrets y en Azure como secretos del contenedor. El RefreshToken se obtiene una sola vez con
/// la herramienta <c>tools/Integraciones.GmailAuth</c>.
/// </summary>
public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>"me" es el usuario dueño del token.</summary>
    public string UserId { get; set; } = "me";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret) && !string.IsNullOrWhiteSpace(RefreshToken);
}
