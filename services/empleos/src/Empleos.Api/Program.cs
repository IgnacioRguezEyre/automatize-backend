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

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<JobOfferService>();
builder.Services.AddScoped<SourceService>();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

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
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapJobOfferEndpoints();
app.MapSourceEndpoints();

app.Run();

// Necesario para que los tests de integración puedan arrancar la API con WebApplicationFactory<Program>.
public partial class Program;
