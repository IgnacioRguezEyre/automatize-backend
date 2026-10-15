using Integraciones.Application.Empleos;
using Integraciones.Application.Mail;
using Integraciones.Application.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integraciones.Application.Sync;

/// <summary>
/// Caso de uso principal: busca en el buzón las alertas de los portales, saca las ofertas con el analizador
/// de cada portal y las manda a Empleos. Solo depende de interfaces (buzón, analizadores y cliente de Empleos),
/// así que se prueba entero sin Gmail ni red.
/// </summary>
public sealed class MailSyncService(
    IMailboxReader mailbox,
    IEnumerable<IJobAlertParser> parsers,
    IEmpleosClient empleos,
    IOptions<SyncOptions> options,
    TimeProvider clock,
    ILogger<MailSyncService> logger)
{
    private const int MaxItemsPerImport = 1000; // límite del endpoint de importación de Empleos

    private readonly IReadOnlyList<IJobAlertParser> _parsers = parsers.ToList();
    private readonly SyncOptions _options = options.Value;

    /// <summary>Consulta de Gmail: alertas de todos los portales de los últimos días.</summary>
    public string BuildQuery()
    {
        var senders = _parsers.SelectMany(p => p.Senders).Distinct(StringComparer.OrdinalIgnoreCase);
        return $"from:({string.Join(" OR ", senders)}) newer_than:{_options.LookbackDays}d";
    }

    /// <param name="dryRun">Si es true, lee y analiza pero no envía nada a Empleos; devuelve las ofertas encontradas.</param>
    public async Task<SyncReport> RunAsync(bool dryRun, CancellationToken cancellationToken)
    {
        var startedAt = clock.GetUtcNow();
        var query = BuildQuery();
        var messages = await mailbox.SearchAsync(query, _options.MaxMessages, cancellationToken);
        logger.LogInformation("Sincronización: {Count} correos para la búsqueda {Query}", messages.Count, query);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
        var items = new Dictionary<string, (ParsedJobOffer Offer, DateOnly ArrivalDate)>(StringComparer.OrdinalIgnoreCase);
        int withOffers = 0, failed = 0;

        // Del más antiguo al más reciente: si una oferta llega en varios correos, cuenta el primero.
        foreach (var message in messages.OrderBy(m => m.ReceivedAt))
        {
            var parser = _parsers.FirstOrDefault(p => p.CanParse(message));
            if (parser is null)
            {
                continue;
            }

            IReadOnlyList<ParsedJobOffer> offers;
            try
            {
                offers = parser.Parse(message);
            }
            catch (Exception e)
            {
                // Un correo con un formato nuevo no debe tumbar la sincronización entera.
                failed++;
                logger.LogWarning(e, "No se pudo analizar el correo {MessageId} de {Portal}", message.Id, parser.Portal);
                continue;
            }

            if (offers.Count == 0)
            {
                continue;
            }

            withOffers++;
            var arrival = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(message.ReceivedAt, timeZone).DateTime);
            foreach (var offer in offers)
            {
                items.TryAdd(offer.ExternalId, (offer, arrival));
            }
        }

        var found = items.Values.Select(v => v.Offer).ToList();
        var byPortal = found.GroupBy(o => o.Portal).ToDictionary(g => g.Key, g => g.Count());

        ImportResult? import = null;
        if (!dryRun && items.Count > 0)
        {
            import = await SendAsync(items.Values.Select(v => ToImportItem(v.Offer, v.ArrivalDate)).ToList(), cancellationToken);
            logger.LogInformation("Empleos: {Created} creadas, {Skipped} ya existían, {Errors} con errores",
                import.Created, import.Skipped, import.Errors.Count);
        }

        return new SyncReport(startedAt, query, dryRun, messages.Count, withOffers, failed, found.Count, byPortal,
            import, dryRun ? found : null);
    }

    private async Task<ImportResult> SendAsync(IReadOnlyList<ImportJobOfferItem> items, CancellationToken cancellationToken)
    {
        int received = 0, created = 0, skipped = 0;
        var errors = new List<ImportError>();
        foreach (var chunk in items.Chunk(MaxItemsPerImport))
        {
            var result = await empleos.ImportAsync(chunk, cancellationToken);
            received += result.Received;
            created += result.Created;
            skipped += result.Skipped;
            errors.AddRange(result.Errors);
        }

        return new ImportResult(received, created, skipped, errors);
    }

    private static ImportJobOfferItem ToImportItem(ParsedJobOffer offer, DateOnly arrivalDate) =>
        new(offer.ExternalId, offer.Title, offer.Company, offer.Portal, arrivalDate,
            offer.Location, offer.WorkMode, offer.Salary, offer.Url, offer.Description);
}
