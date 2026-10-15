using Integraciones.Application.Parsing;
using Integraciones.Application.Parsing.Portals;
using Integraciones.Application.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace Integraciones.Application;

public static class DependencyInjection
{
    /// <summary>Registra los analizadores (uno por portal) y el caso de uso de sincronización.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IJobAlertParser, LinkedInAlertParser>();
        services.AddSingleton<IJobAlertParser, IndeedAlertParser>();
        services.AddSingleton<IJobAlertParser, GlassdoorAlertParser>();
        services.AddSingleton<IJobAlertParser, InfoJobsAlertParser>();
        services.AddSingleton<IJobAlertParser, TecnoempleoAlertParser>();

        services.AddSingleton<SyncGate>();
        services.AddScoped<MailSyncService>(); // scoped: usa el cliente HTTP de Empleos, que no debe vivir para siempre
        return services;
    }
}
