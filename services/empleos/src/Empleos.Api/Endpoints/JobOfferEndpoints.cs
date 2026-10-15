using Empleos.Application.Common;
using Empleos.Application.JobOffers;
using Empleos.Domain.JobOffers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Empleos.Api.Endpoints;

/// <summary>
/// Endpoints de ofertas. Usan TypedResults: el tipo de vuelta de cada handler (por ejemplo
/// Results&lt;Ok&lt;JobOfferDto&gt;, NotFound&gt;) le dice a OpenAPI qué respuestas existen sin tener que declararlas aparte.
/// Los 400 y 409 que salen del manejador de errores de Program.cs no se ven en el tipo, por eso se declaran con ProducesProblem.
/// </summary>
public static class JobOfferEndpoints
{
    public static IEndpointRouteBuilder MapJobOfferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/job-offers").WithTags("Job offers");

        group.MapGet("/", async Task<Results<Ok<PagedResult<JobOfferDto>>, ProblemHttpResult>> (
                string? status, string? search, int? page, int? pageSize, JobOfferService service, CancellationToken ct) =>
            {
                JobOfferStatus? filter = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (!Enum.TryParse<JobOfferStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                        return TypedResults.Problem(
                            title: "Estado no válido",
                            detail: $"Usa uno de estos: {string.Join(", ", Enum.GetNames<JobOfferStatus>())}.",
                            statusCode: StatusCodes.Status400BadRequest);
                    filter = parsed;
                }
                var query = new JobOfferQuery(filter, search, page ?? 1, pageSize ?? JobOfferQuery.DefaultPageSize);
                return TypedResults.Ok(await service.ListAsync(query, ct));
            })
            .WithSummary("Lista las ofertas, paginadas")
            .WithDescription("Filtros opcionales: `status` (New, Viewed, Applied, Discarded) y `search` (busca en título y empresa). " +
                             "`page` empieza en 1 y `pageSize` va de 1 a 100 (20 por defecto). Ordenadas de la más reciente a la más antigua.");

        group.MapGet("/{id:guid}", async Task<Results<Ok<JobOfferDto>, NotFound>> (Guid id, JobOfferService service, CancellationToken ct) =>
                OkOrNotFound(await service.GetAsync(id, ct)))
            .WithName("GetJobOffer")
            .WithSummary("Una oferta, con sus novedades y notas");

        group.MapPost("/", async Task<CreatedAtRoute<JobOfferDto>> (CreateJobOfferRequest request, JobOfferService service, CancellationToken ct) =>
            {
                var offer = await service.CreateAsync(request, ct);
                return TypedResults.CreatedAtRoute(offer, "GetJobOffer", new { id = offer.Id });
            })
            .WithSummary("Crea una oferta")
            .WithDescription("Si no se indica `arrivalDate`, se usa la fecha de hoy. La oferta empieza en estado New.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/import", async Task<Ok<ImportResult>> (IReadOnlyList<ImportJobOfferItem> items, JobOfferService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ImportAsync(items, ct)))
            .WithSummary("Importa ofertas con su historial")
            .WithDescription("Idempotente: las ofertas cuyo `externalId` ya existe se saltan. Las que traen datos no válidos se devuelven en `errors` " +
                             "y no impiden importar las demás. Máximo 1000 por llamada.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        MapTransition(group, "/{id:guid}/view", (o, today) => o.MarkViewed(today), "Marca la oferta como vista");
        MapTransition(group, "/{id:guid}/apply", (o, today) => o.MarkApplied(today), "Marca que has aplicado")
            .ProducesProblem(StatusCodes.Status409Conflict);
        MapTransition(group, "/{id:guid}/unapply", (o, today) => o.UnmarkApplied(today), "Quita la marca de aplicada (vuelve a vista)");
        MapTransition(group, "/{id:guid}/discard", (o, today) => o.Discard(today), "Descarta la oferta");
        MapTransition(group, "/{id:guid}/restore", (o, today) => o.Restore(today), "Recupera una oferta descartada (vuelve a vista)");
        MapTransition(group, "/{id:guid}/close", (o, _) => o.Close(), "Marca que ya no acepta candidaturas");
        MapTransition(group, "/{id:guid}/reopen", (o, _) => o.Reopen(), "Quita la marca de cerrada");

        group.MapPost("/{id:guid}/events", async Task<Results<Ok<JobOfferDto>, NotFound>> (
                Guid id, AddApplicationEventRequest request, JobOfferService service, CancellationToken ct) =>
                OkOrNotFound(await service.AddEventAsync(id, request, ct)))
            .WithSummary("Anota una novedad de la candidatura")
            .WithDescription("Tipos: CvRead, Response, Interview, Rejection. Si la oferta estaba nueva o vista pasa a aplicada; un rechazo la descarta. " +
                             "No se repite el mismo tipo el mismo día.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/details", async Task<Results<Ok<JobOfferDto>, NotFound>> (
                Guid id, UpdateDetailsRequest request, JobOfferService service, CancellationToken ct) =>
                OkOrNotFound(await service.UpdateDetailsAsync(id, request, ct)))
            .WithSummary("Cambia ubicación, modalidad, salario, enlace y descripción")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/notes", async Task<Results<Ok<JobOfferDto>, NotFound>> (
                Guid id, UpdateNotesRequest request, JobOfferService service, CancellationToken ct) =>
                OkOrNotFound(await service.UpdateNotesAsync(id, request, ct)))
            .WithSummary("Guarda las notas de la oferta")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static RouteHandlerBuilder MapTransition(RouteGroupBuilder group, string pattern, Action<JobOffer, DateOnly> change, string summary) =>
        group.MapPost(pattern, async Task<Results<Ok<JobOfferDto>, NotFound>> (Guid id, JobOfferService service, CancellationToken ct) =>
                OkOrNotFound(await service.ChangeAsync(id, change, ct)))
            .WithSummary(summary);

    private static Results<Ok<JobOfferDto>, NotFound> OkOrNotFound(JobOfferDto? offer) =>
        offer is null ? TypedResults.NotFound() : TypedResults.Ok(offer);
}
