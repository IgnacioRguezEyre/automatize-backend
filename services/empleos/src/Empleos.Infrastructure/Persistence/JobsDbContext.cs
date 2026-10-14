using Empleos.Domain.JobOffers;
using Microsoft.EntityFrameworkCore;

namespace Empleos.Infrastructure.Persistence;

public sealed class JobsDbContext(DbContextOptions<JobsDbContext> options) : DbContext(options)
{
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("jobs");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobsDbContext).Assembly);
    }
}
