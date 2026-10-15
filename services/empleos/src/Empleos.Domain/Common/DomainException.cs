namespace Empleos.Domain.Common;

/// <summary>
/// Se lanza cuando una operación incumple una regla de negocio (por ejemplo, aplicar a una oferta descartada).
/// La API la convierte en 409 Conflict. Hereda de InvalidOperationException para que el significado siga siendo
/// "la operación no es válida en el estado actual".
/// </summary>
public sealed class DomainException(string message) : InvalidOperationException(message);
