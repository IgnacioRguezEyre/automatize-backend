using Empleos.Domain.JobOffers;
using Empleos.Domain.Sources;
using Microsoft.EntityFrameworkCore;

namespace Empleos.Infrastructure.Persistence;

public sealed class JobsDbContext(DbContextOptions<JobsDbContext> options) : DbContext(options)
{
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<Source> Sources => Set<Source>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("jobs");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobsDbContext).Assembly);
    }
}
