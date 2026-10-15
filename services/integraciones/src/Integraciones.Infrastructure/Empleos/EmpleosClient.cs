using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Integraciones.Application.Empleos;

namespace Integraciones.Infrastructure.Empleos;

/// <summary>
/// Cliente HTTP "tipado" de Empleos: la fábrica de HttpClient le inyecta un HttpClient ya configurado
/// (URL base, cabecera de la clave y reintentos), así esta clase solo se ocupa de la llamada en sí.
/// </summary>
public sealed class EmpleosClient(HttpClient http) : IEmpleosClient
{
    public const string ApiKeyHeader = "X-Api-Key";

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ImportResult> ImportAsync(IReadOnlyList<ImportJobOfferItem> items, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("api/job-offers/import", items, Json, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new EmpleosApiException(response.StatusCode, body);
        }

        return await response.Content.ReadFromJsonAsync<ImportResult>(Json, cancellationToken)
               ?? throw new EmpleosApiException(response.StatusCode, "Respuesta vacía");
    }
}

/// <summary>Empleos respondió con un error que no se arregla reintentando (o se agotaron los reintentos).</summary>
public sealed class EmpleosApiException(HttpStatusCode statusCode, string body)
    : Exception($"Empleos respondió {(int)statusCode} ({statusCode}): {body}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
