using Integraciones.Application.Empleos;
using Integraciones.Application.Mail;
using Integraciones.Infrastructure.Empleos;
using Integraciones.Infrastructure.Gmail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Integraciones.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra los adaptadores reales: Gmail para leer el buzón y HTTP para hablar con Empleos.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GmailOptions>(configuration.GetSection(GmailOptions.SectionName));
        services.AddSingleton<IMailboxReader, GmailMailboxReader>();

        services.AddOptions<EmpleosOptions>()
            .Bind(configuration.GetSection(EmpleosOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "Empleos:BaseUrl tiene que ser una URL absoluta.")
            .ValidateOnStart(); // si falta, la aplicación no arranca: mejor enterarse al desplegar que en la primera sincronización

        services.AddHttpClient<IEmpleosClient, EmpleosClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<EmpleosOptions>>().Value;
                // La barra final importa: sin ella, "api/job-offers/import" sustituiría el último tramo de la URL base.
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Add(EmpleosClient.ApiKeyHeader, options.ApiKey);
                }
            })
            // Reintentos con espera exponencial ante errores transitorios (5xx, 408, 429, caídas de red), timeout por
            // intento y total, y circuit breaker. Reintentar el POST es seguro porque la importación es idempotente.
            .AddStandardResilienceHandler();

        return services;
    }
}
