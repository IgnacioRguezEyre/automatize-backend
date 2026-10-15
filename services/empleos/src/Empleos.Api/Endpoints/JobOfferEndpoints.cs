using Empleos.Application.JobOffers;
using Empleos.Domain.JobOffers;

namespace Empleos.Api.Endpoints;

public static class JobOfferEndpoints
{
    public static IEndpointRouteBuilder MapJobOfferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/job-offers").WithTags("Job offers");

        // GET /api/job-offers?status=Applied&search=net&page=1&pageSize=20
        group.MapGet("/", async (string? status, string? search, int? page, int? pageSize, JobOfferService service, CancellationToken ct) =>
        {
            JobOfferStatus? filter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<JobOfferStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                    return Results.Problem(
                        title: "Estado no válido",
                        detail: $"Usa uno de estos: {string.Join(", ", Enum.GetNames<JobOfferStatus>())}.",
                        statusCode: StatusCodes.Status400BadRequest);
                filter = parsed;
            }
            var query = new JobOfferQuery(filter, search, page ?? 1, pageSize ?? JobOfferQuery.DefaultPageSize);
            return Results.Ok(await service.ListAsync(query, ct));
        });

        // Importación idempotente: las ofertas cuyo externalId ya existe se saltan.
        group.MapPost("/import", async (IReadOnlyList<ImportJobOfferItem> items, JobOfferService service, CancellationToken ct) =>
            Results.Ok(await service.ImportAsync(items, ct)));

        group.MapGet("/{id:guid}", async (Guid id, JobOfferService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } offer ? Results.Ok(offer) : Results.NotFound())
            .WithName("GetJobOffer");

        group.MapPost("/", async (CreateJobOfferRequest request, JobOfferService service, CancellationToken ct) =>
        {
            var offer = await service.CreateAsync(request, ct);
            return Results.CreatedAtRoute("GetJobOffer", new { id = offer.Id }, offer);
        });

        MapTransition(group, "/{id:guid}/view", (o, today) => o.MarkViewed(today));
        MapTransition(group, "/{id:guid}/apply", (o, today) => o.MarkApplied(today));
        MapTransition(group, "/{id:guid}/unapply", (o, today) => o.UnmarkApplied(today));
        MapTransition(group, "/{id:guid}/discard", (o, today) => o.Discard(today));
        MapTransition(group, "/{id:guid}/restore", (o, today) => o.Restore(today));
        MapTransition(group, "/{id:guid}/close", (o, _) => o.Close());
        MapTransition(group, "/{id:guid}/reopen", (o, _) => o.Reopen());

        group.MapPost("/{id:guid}/events", async (Guid id, AddApplicationEventRequest request, JobOfferService service, CancellationToken ct) =>
            await service.AddEventAsync(id, request, ct) is { } offer ? Results.Ok(offer) : Results.NotFound());

        group.MapPut("/{id:guid}/details", async (Guid id, UpdateDetailsRequest request, JobOfferService service, CancellationToken ct) =>
            await service.UpdateDetailsAsync(id, request, ct) is { } offer ? Results.Ok(offer) : Results.NotFound());

        group.MapPut("/{id:guid}/notes", async (Guid id, UpdateNotesRequest request, JobOfferService service, CancellationToken ct) =>
            await service.UpdateNotesAsync(id, request, ct) is { } offer ? Results.Ok(offer) : Results.NotFound());

        return app;
    }

    private static void MapTransition(RouteGroupBuilder group, string pattern, Action<JobOffer, DateOnly> change) =>
        group.MapPost(pattern, async (Guid id, JobOfferService service, CancellationToken ct) =>
            await service.ChangeAsync(id, change, ct) is { } offer ? Results.Ok(offer) : Results.NotFound());
}
