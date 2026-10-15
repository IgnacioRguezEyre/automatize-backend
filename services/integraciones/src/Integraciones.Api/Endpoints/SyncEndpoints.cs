using Integraciones.Application.Sync;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Integraciones.Api.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync").WithTags("Sincronización");

        group.MapPost("/", async Task<Results<Ok<SyncReport>, Conflict<string>>> (
                bool? dryRun, MailSyncService sync, SyncGate gate, CancellationToken ct) =>
            {
                var report = await gate.TryRunAsync(() => sync.RunAsync(dryRun ?? false, ct));
                return report is null
                    ? TypedResults.Conflict("Ya hay una sincronización en marcha. Prueba en unos segundos.")
                    : TypedResults.Ok(report);
            })
            .WithSummary("Lee el correo y envía las ofertas nuevas a Empleos")
            .WithDescription("Con `dryRun=true` solo lee y analiza: devuelve las ofertas encontradas sin enviar nada. " +
                             "Útil para comprobar un analizador.");

        group.MapGet("/query", (MailSyncService sync) => TypedResults.Ok(sync.BuildQuery()))
            .WithSummary("Búsqueda de Gmail que se usa en cada sincronización");

        return app;
    }
}
