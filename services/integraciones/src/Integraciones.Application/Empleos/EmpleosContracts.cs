namespace Integraciones.Application.Empleos;

// Copia del contrato de POST /api/job-offers/import de Empleos, solo con los campos que se envían.
// No se referencia el proyecto de Empleos a propósito: cada microservicio se compila y despliega por
// separado, y lo único que comparten es el formato JSON. Los campos que faltan (estado, notas, historial)
// Empleos los toma con su valor por defecto: la oferta entra como nueva.

/// <summary>Una oferta tal como la espera el endpoint de importación de Empleos.</summary>
public sealed record ImportJobOfferItem(
    string ExternalId,
    string Title,
    string? Company,
    string Portal,
    DateOnly ArrivalDate,
    string? Location,
    string? WorkMode,
    string? Salary,
    string? Url,
    string? Description);

/// <summary>Respuesta de la importación: cuántas se crearon, cuántas ya existían y cuáles fallaron.</summary>
public sealed record ImportResult(int Received, int Created, int Skipped, IReadOnlyList<ImportError> Errors);

public sealed record ImportError(string ExternalId, string Message);
