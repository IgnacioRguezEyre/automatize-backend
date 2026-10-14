using System.Text.Json;
using System.Text.Json.Serialization;

namespace Empleos.Api.IntegrationTests;

/// <summary>Mismas opciones que la API: nombres en camelCase y enums como texto.</summary>
internal static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
