using Integraciones.Api.Endpoints;
using Integraciones.Api.Security;
using Integraciones.Api.Workers;
using Integraciones.Application;
using Integraciones.Application.Sync;
using Integraciones.Infrastructure;
using Integraciones.Infrastructure.Empleos;
using Integraciones.Infrastructure.Gmail;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Automatize · Integraciones API";
    document.Info.Version = "v1";
    document.Info.Description = "Lee las alertas de empleo del correo (Gmail) y las envía al servicio Empleos.";
    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection(SyncOptions.SectionName));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MailSyncWorker>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Los fallos de los sistemas externos se devuelven con un código que dice de quién es la culpa.
app.UseExceptionHandler(errors => errors.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = error switch
    {
        GmailNotConfiguredException => (StatusCodes.Status503ServiceUnavailable, "Gmail no está configurado"),
        EmpleosApiException or HttpRequestException => (StatusCodes.Status502BadGateway, "Empleos no respondió bien"),
        _ => (StatusCodes.Status500InternalServerError, "Error inesperado")
    };
    context.Response.StatusCode = status;
    await Results.Problem(title: title, detail: status != StatusCodes.Status500InternalServerError ? error?.Message : null, statusCode: status)
        .ExecuteAsync(context);
}));
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Integraciones API v1");
        options.DocumentTitle = "Integraciones API";
        options.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapHealthChecks("/health");
app.MapSyncEndpoints();

app.Run();

// Para los tests con WebApplicationFactory<Program>.
public partial class Program;
