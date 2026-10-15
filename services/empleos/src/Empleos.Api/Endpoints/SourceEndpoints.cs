using Empleos.Application.Sources;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Empleos.Api.Endpoints;

public static class SourceEndpoints
{
    public static IEndpointRouteBuilder MapSourceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sources").WithTags("Sources");

        group.MapGet("/", async Task<Ok<IReadOnlyList<SourceDto>>> (SourceService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListAsync(ct)))
            .WithSummary("Lista las fuentes, por nombre");

        group.MapGet("/{id:guid}", async Task<Results<Ok<SourceDto>, NotFound>> (Guid id, SourceService service, CancellationToken ct) =>
                OkOrNotFound(await service.GetAsync(id, ct)))
            .WithName("GetSource")
            .WithSummary("Una fuente");

        group.MapPost("/", async Task<CreatedAtRoute<SourceDto>> (CreateSourceRequest request, SourceService service, CancellationToken ct) =>
            {
                var source = await service.CreateAsync(request, ct);
                return TypedResults.CreatedAtRoute(source, "GetSource", new { id = source.Id });
            })
            .WithSummary("Crea una fuente")
            .WithDescription("El nombre no se puede repetir (409). Los remitentes se guardan en minúsculas; uno no válido devuelve 400.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/senders", async Task<Results<Ok<SourceDto>, NotFound>> (
                Guid id, AddSenderRequest request, SourceService service, CancellationToken ct) =>
                OkOrNotFound(await service.AddSenderAsync(id, request, ct)))
            .WithSummary("Añade un remitente")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/senders/{email}", async Task<Results<Ok<SourceDto>, NotFound>> (
                Guid id, string email, SourceService service, CancellationToken ct) =>
                OkOrNotFound(await service.RemoveSenderAsync(id, email, ct)))
            .WithSummary("Quita un remitente");

        group.MapPost("/{id:guid}/activate", async Task<Results<Ok<SourceDto>, NotFound>> (Guid id, SourceService service, CancellationToken ct) =>
                OkOrNotFound(await service.ActivateAsync(id, ct)))
            .WithSummary("Activa la fuente");

        group.MapPost("/{id:guid}/deactivate", async Task<Results<Ok<SourceDto>, NotFound>> (Guid id, SourceService service, CancellationToken ct) =>
                OkOrNotFound(await service.DeactivateAsync(id, ct)))
            .WithSummary("Desactiva la fuente (la lectura del correo la ignora)");

        group.MapPut("/{id:guid}/rule", async Task<Results<Ok<SourceDto>, NotFound>> (
                Guid id, UpdateRuleRequest request, SourceService service, CancellationToken ct) =>
                OkOrNotFound(await service.UpdateRuleAsync(id, request, ct)))
            .WithSummary("Cambia la regla de filtrado")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, SourceService service, CancellationToken ct) =>
                await service.DeleteAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithSummary("Borra la fuente");

        return app;
    }

    private static Results<Ok<SourceDto>, NotFound> OkOrNotFound(SourceDto? source) =>
        source is null ? TypedResults.NotFound() : TypedResults.Ok(source);
}
