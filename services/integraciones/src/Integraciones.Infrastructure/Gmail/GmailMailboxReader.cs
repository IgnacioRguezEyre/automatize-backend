using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Integraciones.Application.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integraciones.Infrastructure.Gmail;

/// <summary>
/// Lee el buzón con la API de Gmail. Se autentica con OAuth 2.0: con el refresh token (permanente mientras
/// no se revoque) la librería de Google pide un access token de una hora cada vez que hace falta.
/// El permiso es solo de lectura (<c>gmail.readonly</c>): el servicio no puede borrar ni enviar correos.
/// </summary>
public sealed class GmailMailboxReader : IMailboxReader, IDisposable
{
    private const int PageSize = 100;

    private readonly GmailService _gmail;
    private readonly string _userId;
    private readonly ILogger<GmailMailboxReader> _logger;

    public GmailMailboxReader(IOptions<GmailOptions> options, ILogger<GmailMailboxReader> logger)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new GmailNotConfiguredException(
                "Faltan las credenciales de Gmail (Gmail:ClientId, Gmail:ClientSecret y Gmail:RefreshToken). " +
                "Consulta docs/gmail-setup.md.");
        }

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = settings.ClientId, ClientSecret = settings.ClientSecret },
            Scopes = [GmailService.Scope.GmailReadonly]
        });
        var credential = new UserCredential(flow, settings.UserId, new TokenResponse { RefreshToken = settings.RefreshToken });

        _gmail = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Automatize Integraciones"
        });
        _userId = settings.UserId;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmailMessage>> SearchAsync(string query, int maxMessages, CancellationToken cancellationToken)
    {
        // 1) La búsqueda solo devuelve ids, paginados.
        var ids = new List<string>();
        string? pageToken = null;
        do
        {
            var request = _gmail.Users.Messages.List(_userId);
            request.Q = query;
            request.MaxResults = Math.Min(PageSize, maxMessages - ids.Count);
            request.PageToken = pageToken;

            var page = await request.ExecuteAsync(cancellationToken);
            ids.AddRange(page.Messages?.Select(m => m.Id) ?? []);
            pageToken = page.NextPageToken;
        }
        while (pageToken is not null && ids.Count < maxMessages);

        // 2) Cada correo se descarga entero (cabeceras y cuerpos). Unos pocos a la vez para no pasarse de la cuota.
        var messages = new EmailMessage[ids.Count];
        await Parallel.ForEachAsync(
            ids.Select((id, index) => (id, index)),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
            async (item, ct) =>
            {
                var request = _gmail.Users.Messages.Get(_userId, item.id);
                request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                messages[item.index] = GmailMessageMapper.ToEmailMessage(await request.ExecuteAsync(ct));
            });

        _logger.LogDebug("Gmail devolvió {Count} correos", messages.Length);
        return messages;
    }

    public void Dispose() => _gmail.Dispose();
}

/// <summary>Faltan las credenciales OAuth de Gmail en la configuración.</summary>
public sealed class GmailNotConfiguredException(string message) : InvalidOperationException(message);
