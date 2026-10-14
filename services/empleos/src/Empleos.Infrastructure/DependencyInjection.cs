using Empleos.Application.JobOffers;
using Empleos.Application.Sources;
using Empleos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Empleos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<JobsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "jobs")));
        services.AddScoped<IJobOfferRepository, JobOfferRepository>();
        services.AddScoped<ISourceRepository, SourceRepository>();
        return services;
    }
}
