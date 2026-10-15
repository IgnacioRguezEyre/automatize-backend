using Integraciones.Application.Sync;
using Microsoft.Extensions.Options;

namespace Integraciones.Api.Workers;

/// <summary>
/// Servicio en segundo plano (BackgroundService) que lanza la sincronización cada <c>Sync:IntervalMinutes</c>.
/// Con 0 queda apagado y solo se sincroniza llamando a <c>POST /api/sync</c>.
/// </summary>
public sealed class MailSyncWorker(
    IServiceScopeFactory scopes,
    SyncGate gate,
    IOptions<SyncOptions> options,
    ILogger<MailSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = options.Value.IntervalMinutes;
        if (minutes <= 0)
        {
            logger.LogInformation("Sincronización automática desactivada (Sync:IntervalMinutes = {Minutes})", minutes);
            return;
        }

        // PeriodicTimer no acumula ticks si una pasada tarda más que el intervalo.
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        do
        {
            try
            {
                // El worker es singleton y MailSyncService es scoped: cada pasada abre su propio ámbito.
                await using var scope = scopes.CreateAsyncScope();
                var sync = scope.ServiceProvider.GetRequiredService<MailSyncService>();
                var report = await gate.TryRunAsync(() => sync.RunAsync(dryRun: false, stoppingToken));
                if (report is null)
                {
                    logger.LogInformation("Se salta la pasada automática: ya hay una sincronización en marcha");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                // Sin Gmail configurado, Empleos caído... se registra y se reintenta en la siguiente vuelta.
                logger.LogError(e, "Falló la sincronización automática");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
