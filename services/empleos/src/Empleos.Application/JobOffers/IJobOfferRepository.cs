using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

/// <summary>Puerto de persistencia. La implementación vive en Infrastructure.</summary>
public interface IJobOfferRepository
{
    Task<JobOffer?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<JobOffer>> ListAsync(JobOfferStatus? status, CancellationToken ct);
    void Add(JobOffer offer);
    Task SaveChangesAsync(CancellationToken ct);
}
