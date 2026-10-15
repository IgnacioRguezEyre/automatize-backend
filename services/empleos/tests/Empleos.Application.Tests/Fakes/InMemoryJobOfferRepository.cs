using Empleos.Application.JobOffers;
using Empleos.Domain.JobOffers;

namespace Empleos.Application.Tests.Fakes;

/// <summary>Repositorio en memoria: prueba los casos de uso sin base de datos. Cuenta cuántas veces se guarda.</summary>
internal sealed class InMemoryJobOfferRepository : IJobOfferRepository
{
    public List<JobOffer> Offers { get; } = [];
    public int SaveCount { get; private set; }

    public Task<JobOffer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Offers.FirstOrDefault(o => o.Id == id));

    public Task<(IReadOnlyList<JobOffer> Items, int TotalCount)> ListAsync(JobOfferQuery query, CancellationToken ct)
    {
        var filtered = Offers
            .Where(o => query.Status is null || o.Status == query.Status)
            .Where(o => query.Search is null
                || o.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                || o.Company.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.ArrivalDate).ThenBy(o => o.Company).ThenBy(o => o.Id)
            .ToList();
        IReadOnlyList<JobOffer> page = filtered.Skip(query.Skip).Take(query.PageSize).ToList();
        return Task.FromResult((page, filtered.Count));
    }

    public Task<IReadOnlySet<string>> GetExistingExternalIdsAsync(IReadOnlyCollection<string> externalIds, CancellationToken ct)
    {
        var existing = Offers.Where(o => o.ExternalId is not null && externalIds.Contains(o.ExternalId, StringComparer.OrdinalIgnoreCase))
            .Select(o => o.ExternalId!);
        return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase));
    }

    public void Add(JobOffer offer) => Offers.Add(offer);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
