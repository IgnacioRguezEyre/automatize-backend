using Empleos.Application.JobOffers;
using Empleos.Domain.JobOffers;

namespace Empleos.Api.Endpoints;

public static class JobOfferEndpoints
{
    public static IEndpointRouteBuilder MapJobOfferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/job-offers").WithTags("Job offers");

        group.MapGet("/", async (JobOfferStatus? status, JobOfferService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(status, ct)));

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

        return app;
    }

    private static void MapTransition(RouteGroupBuilder group, string pattern, Action<JobOffer, DateOnly> change) =>
        group.MapPost(pattern, async (Guid id, JobOfferService service, CancellationToken ct) =>
            await service.ChangeAsync(id, change, ct) is { } offer ? Results.Ok(offer) : Results.NotFound());
}
