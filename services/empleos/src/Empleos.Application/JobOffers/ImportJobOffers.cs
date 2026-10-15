using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

/// <summary>
/// Una oferta tal como viene de otro sistema (la página actual de Automatize o, más adelante, el servicio
/// que lee el correo), con su historial: estado, novedades, notas y si está cerrada.
/// </summary>
public sealed record ImportJobOfferItem(
    string ExternalId,
    string Title,
    string? Company,
    string Portal,
    DateOnly ArrivalDate,
    JobOfferStatus? Status,
    DateOnly? StatusDate,
    string? Location,
    string? WorkMode,
    string? Salary,
    string? Url,
    string? Description,
    string? Notes,
    bool IsClosed,
    IReadOnlyList<ImportApplicationEvent>? Events);

public sealed record ImportApplicationEvent(DateOnly Date, ApplicationEventType Type, string? Text);

public sealed record ImportResult(int Received, int Created, int Skipped, IReadOnlyList<ImportError> Errors);

public sealed record ImportError(string ExternalId, string Message);
