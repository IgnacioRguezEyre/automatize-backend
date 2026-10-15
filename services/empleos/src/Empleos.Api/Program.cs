using System.Text.Json.Serialization;
using Empleos.Api.Endpoints;
using Empleos.Application.JobOffers;
using Empleos.Application.Sources;
using Empleos.Domain.Common;
using Empleos.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Jobs")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Jobs'. Guárdala con dotnet user-secrets.");

// Documento OpenAPI (la descripción de la API en JSON). Lo genera el propio ASP.NET Core a partir de los endpoints.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Automatize · Empleos API";
    document.Info.Version = "v1";
    document.Info.Description = "Ofertas de empleo, seguimiento de candidaturas y fuentes de las que llegan.";
    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<JobOfferService>();
builder.Services.AddScoped<SourceService>();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

// En desarrollo la base de datos se crea o actualiza sola al arrancar (ver appsettings.Development.json).
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateJobsDatabaseAsync();
}

// Las reglas de dominio que se incumplen se devuelven como 400 o 409 en lugar de 500.
app.UseExceptionHandler(errors => errors.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = error switch
    {
        BadHttpRequestException bad => (bad.StatusCode, "Petición no válida"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Datos no válidos"),
        DomainException => (StatusCodes.Status409Conflict, "Operación no permitida"),
        _ => (StatusCodes.Status500InternalServerError, "Error inesperado")
    };
    context.Response.StatusCode = status;
    await Results.Problem(title: title, detail: status < 500 ? error?.Message : null, statusCode: status).ExecuteAsync(context);
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // sirve el documento en /openapi/v1.json

    // Swagger UI es solo la interfaz web que lee ese documento y permite probar la API desde el navegador.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Empleos API v1");
        options.DocumentTitle = "Empleos API";
        options.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.MapJobOfferEndpoints();
app.MapSourceEndpoints();

app.Run();

// Necesario para que los tests de integración puedan arrancar la API con WebApplicationFactory<Program>.
public partial class Program;
