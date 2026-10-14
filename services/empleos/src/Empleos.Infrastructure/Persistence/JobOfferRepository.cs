using Empleos.Application.JobOffers;
using Empleos.Domain.JobOffers;
using Microsoft.EntityFrameworkCore;

namespace Empleos.Infrastructure.Persistence;

internal sealed class JobOfferRepository(JobsDbContext db) : IJobOfferRepository
{
    public Task<JobOffer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.JobOffers.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<JobOffer>> ListAsync(JobOfferStatus? status, CancellationToken ct)
    {
        var query = db.JobOffers.AsNoTracking();
        if (status is not null) query = query.Where(o => o.Status == status);
        return await query.OrderByDescending(o => o.ArrivalDate).ThenBy(o => o.Company).ToListAsync(ct);
    }

    public void Add(JobOffer offer) => db.JobOffers.Add(offer);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
