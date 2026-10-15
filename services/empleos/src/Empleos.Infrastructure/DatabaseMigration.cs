using Empleos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Empleos.Infrastructure;

public static class DatabaseMigration
{
    /// <summary>
    /// Aplica las migraciones pendientes. Pensado para desarrollo y para levantar el proyecto de cero con Docker;
    /// en producción las migraciones las aplica el pipeline de despliegue, no la API al arrancar.
    /// </summary>
    public static async Task MigrateJobsDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<JobsDbContext>().Database.MigrateAsync(ct);
    }
}
