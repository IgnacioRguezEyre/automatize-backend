using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

/// <summary>Puerto de persistencia. La implementación vive en Infrastructure.</summary>
public interface IJobOfferRepository
{
    Task<JobOffer?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Devuelve la página pedida y el total de ofertas que cumplen los filtros.</summary>
    Task<(IReadOnlyList<JobOffer> Items, int TotalCount)> ListAsync(JobOfferQuery query, CancellationToken ct);

    /// <summary>De los identificadores externos dados, los que ya existen.</summary>
    Task<IReadOnlySet<string>> GetExistingExternalIdsAsync(IReadOnlyCollection<string> externalIds, CancellationToken ct);

    void Add(JobOffer offer);
    Task SaveChangesAsync(CancellationToken ct);
}
