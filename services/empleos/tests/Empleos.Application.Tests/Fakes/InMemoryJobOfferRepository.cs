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

    public Task<IReadOnlyList<JobOffer>> ListAsync(JobOfferStatus? status, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<JobOffer>>(Offers.Where(o => status is null || o.Status == status).ToList());

    public void Add(JobOffer offer) => Offers.Add(offer);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
