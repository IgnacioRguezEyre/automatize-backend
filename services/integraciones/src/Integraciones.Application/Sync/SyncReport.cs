using Integraciones.Application.Empleos;
using Integraciones.Application.Parsing;

namespace Integraciones.Application.Sync;

/// <summary>Resumen de una pasada de sincronización. Es lo que devuelve <c>POST /api/sync</c>.</summary>
/// <param name="MessagesRead">Correos que devolvió el buzón.</param>
/// <param name="MessagesWithOffers">Correos de los que salió al menos una oferta.</param>
/// <param name="MessagesFailed">Correos cuyo analizador falló (se registran en el log y se sigue con los demás).</param>
/// <param name="OffersFound">Ofertas distintas encontradas entre todos los correos.</param>
/// <param name="Import">Respuesta de Empleos. Null si no había nada que enviar o era una simulación.</param>
/// <param name="Offers">Las ofertas encontradas; solo se rellenan en modo simulación (<c>dryRun</c>).</param>
public sealed record SyncReport(
    DateTimeOffset StartedAt,
    string Query,
    bool DryRun,
    int MessagesRead,
    int MessagesWithOffers,
    int MessagesFailed,
    int OffersFound,
    IReadOnlyDictionary<string, int> OffersByPortal,
    ImportResult? Import,
    IReadOnlyList<ParsedJobOffer>? Offers);
