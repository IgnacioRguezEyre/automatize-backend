using System.Security.Cryptography;
using System.Text;

namespace Empleos.Api.Security;

/// <summary>
/// Protección mínima mientras no exista el servicio de identidad: si hay una clave configurada en
/// <c>Security:ApiKey</c>, toda petición a <c>/api</c> tiene que traerla en la cabecera <c>X-Api-Key</c>.
/// Sin clave configurada (desarrollo y tests) la API queda abierta.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var expected = configuration["Security:ApiKey"];
        if (string.IsNullOrEmpty(expected) || !context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var received = context.Request.Headers[HeaderName].ToString();
        // Comparación en tiempo constante: no deja deducir la clave midiendo cuánto tarda en fallar.
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(received), Encoding.UTF8.GetBytes(expected)))
        {
            await Results.Problem(
                title: "No autorizado",
                detail: $"Falta la cabecera {HeaderName} o no es válida.",
                statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}
