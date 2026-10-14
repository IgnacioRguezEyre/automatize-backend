using Empleos.Application.Sources;

namespace Empleos.Api.Endpoints;

public static class SourceEndpoints
{
    public static IEndpointRouteBuilder MapSourceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sources").WithTags("Sources");

        group.MapGet("/", async (SourceService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, SourceService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } source ? Results.Ok(source) : Results.NotFound())
            .WithName("GetSource");

        group.MapPost("/", async (CreateSourceRequest request, SourceService service, CancellationToken ct) =>
        {
            var source = await service.CreateAsync(request, ct);
            return Results.CreatedAtRoute("GetSource", new { id = source.Id }, source);
        });

        group.MapPost("/{id:guid}/senders", async (Guid id, AddSenderRequest request, SourceService service, CancellationToken ct) =>
            ToResult(await service.AddSenderAsync(id, request, ct)));

        group.MapDelete("/{id:guid}/senders/{email}", async (Guid id, string email, SourceService service, CancellationToken ct) =>
            ToResult(await service.RemoveSenderAsync(id, email, ct)));

        group.MapPost("/{id:guid}/activate", async (Guid id, SourceService service, CancellationToken ct) =>
            ToResult(await service.ActivateAsync(id, ct)));

        group.MapPost("/{id:guid}/deactivate", async (Guid id, SourceService service, CancellationToken ct) =>
            ToResult(await service.DeactivateAsync(id, ct)));

        group.MapPut("/{id:guid}/rule", async (Guid id, UpdateRuleRequest request, SourceService service, CancellationToken ct) =>
            ToResult(await service.UpdateRuleAsync(id, request, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, SourceService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }

    private static IResult ToResult(SourceDto? source) => source is null ? Results.NotFound() : Results.Ok(source);
}
