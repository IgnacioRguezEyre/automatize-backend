using Empleos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Empleos.Api.IntegrationTests;

/// <summary>
/// Arranca la API de verdad en memoria contra un SQL Server de usar y tirar en Docker (Testcontainers).
/// Antes de los tests aplica las migraciones, así que también comprueba que están al día con el modelo.
/// Necesita Docker Desktop arrancado.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _database =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        var connectionString = new SqlConnectionStringBuilder(_database.GetConnectionString())
        {
            InitialCatalog = "Jobs"
        }.ConnectionString;

        // Program.cs lee la cadena de conexión nada más arrancar, así que se pasa como variable de entorno,
        // que tiene prioridad sobre appsettings y los user-secrets.
        Environment.SetEnvironmentVariable("ConnectionStrings__Jobs", connectionString);

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<JobsDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}

/// <summary>Todas las clases de test de la API comparten un único contenedor.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
