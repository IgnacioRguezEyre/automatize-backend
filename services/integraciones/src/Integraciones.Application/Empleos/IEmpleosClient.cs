namespace Integraciones.Application.Empleos;

/// <summary>Puerto de salida hacia el microservicio Empleos. Hoy por HTTP; en la sesión 4, por mensajes.</summary>
public interface IEmpleosClient
{
    Task<ImportResult> ImportAsync(IReadOnlyList<ImportJobOfferItem> items, CancellationToken cancellationToken);
}
