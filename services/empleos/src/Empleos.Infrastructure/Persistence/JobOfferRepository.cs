using Empleos.Application.JobOffers;
using Empleos.Domain.JobOffers;
using Microsoft.EntityFrameworkCore;

namespace Empleos.Infrastructure.Persistence;

internal sealed class JobOfferRepository(JobsDbContext db) : IJobOfferRepository
{
    public Task<JobOffer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.JobOffers.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(IReadOnlyList<JobOffer> Items, int TotalCount)> ListAsync(JobOfferQuery query, CancellationToken ct)
    {
        var offers = db.JobOffers.AsNoTracking();

        if (query.Status is { } status)
            offers = offers.Where(o => o.Status == status);

        // SQL Server compara sin distinguir mayúsculas (collation CI), así que "net" encuentra ".NET".
        if (query.Search is { } search)
            offers = offers.Where(o => o.Title.Contains(search) || o.Company.Contains(search));

        var total = await offers.CountAsync(ct);

        // El Id al final hace el orden estable: sin él, dos ofertas del mismo día y empresa
        // podrían cambiar de página entre una petición y otra.
        var items = await offers
            .OrderByDescending(o => o.ArrivalDate).ThenBy(o => o.Company).ThenBy(o => o.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlySet<string>> GetExistingExternalIdsAsync(IReadOnlyCollection<string> externalIds, CancellationToken ct)
    {
        if (externalIds.Count == 0) return new HashSet<string>();

        var existing = await db.JobOffers
            .Where(o => o.ExternalId != null && externalIds.Contains(o.ExternalId))
            .Select(o => o.ExternalId!)
            .ToListAsync(ct);

        return new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
    }

    public void Add(JobOffer offer) => db.JobOffers.Add(offer);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
