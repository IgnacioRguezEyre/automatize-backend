namespace Integraciones.Application.Parsing;

/// <summary>Una oferta extraída de un correo de alerta. Solo lo que el portal cuenta en el propio correo.</summary>
/// <param name="ExternalId">
/// Identificador estable de la oferta en su portal, con prefijo (<c>li-</c>, <c>gd-</c>...). Es lo que hace que
/// importar dos veces el mismo correo no duplique nada en Empleos.
/// </param>
public sealed record ParsedJobOffer(
    string ExternalId,
    string Portal,
    string Title,
    string? Company,
    string? Location,
    string? WorkMode,
    string? Salary,
    string? Url,
    string? Description);
